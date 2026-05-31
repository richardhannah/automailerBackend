using AutoMailerBackend.Auth;
using AutoMailerBackend.Clients;
using AutoMailerBackend.Data;
using AutoMailerBackend.Models;
using AutoMailerBackend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoMailerBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[TokenAuth]
public class AccountController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly LoginService _loginService;
    private readonly SubscriptionsService _subscriptionsService;
    private readonly Smtp2GoClient _smtpClient;
    private readonly ILogger<AccountController> _logger;

    public AccountController(AppDbContext db, LoginService loginService, SubscriptionsService subscriptionsService, Smtp2GoClient smtpClient, ILogger<AccountController> logger)
    {
        _db = db;
        _loginService = loginService;
        _subscriptionsService = subscriptionsService;
        _smtpClient = smtpClient;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult GetAccount()
    {
        var user = HttpContext.Items["User"] as User;
        var login = HttpContext.Items["Login"] as Login;

        return Ok(new
        {
            username = login!.Username,
            email = user!.Email,
            role = user.Role.ToString()
        });
    }

    [HttpGet("subscriptions")]
    public async Task<IActionResult> GetMySubscriptions()
    {
        var user = HttpContext.Items["User"] as User;

        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.UserId == user!.UserId);
        if (customer == null)
            return Ok(Array.Empty<object>());

        var subscriptions = await _subscriptionsService.GetByCustomerIdAsync(customer.CustomerId);

        var result = subscriptions.Select(s => new
        {
            s.SubscriptionId,
            PackageName = s.IptvPackage.PackageName,
            Price = s.IptvPackage.Price,
            BillingPeriod = s.IptvPackage.BillingPeriod.ToString(),
            s.DateStarted,
            s.DateEnded,
            Status = s.Status.ToString()
        });

        return Ok(result);
    }

    [HttpPost("subscriptions/{id}/cancel")]
    public async Task<IActionResult> CancelSubscription(int id)
    {
        var user = HttpContext.Items["User"] as User;

        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.UserId == user!.UserId);
        if (customer == null)
            return NotFound(new { error = "No customer record found" });

        var subscription = await _subscriptionsService.GetByIdAsync(id);
        if (subscription == null || subscription.CustomerId != customer.CustomerId)
            return NotFound(new { error = "Subscription not found" });

        if (subscription.Status == SubscriptionStatus.Cancelled)
            return BadRequest(new { error = "Subscription is already cancelled" });

        var result = await _subscriptionsService.UpdateAsync(id, null, "Cancelled", DateTime.UtcNow);

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        // Send cancellation emails
        await SendCancellationConfirmationAsync(customer, subscription.IptvPackage);
        await SendCancellationAdminNotificationAsync(customer, subscription.IptvPackage);

        return Ok(new { message = "Subscription cancelled" });
    }

    [HttpPost("request-password-reset")]
    public async Task<IActionResult> RequestPasswordReset()
    {
        var user = HttpContext.Items["User"] as User;
        var login = HttpContext.Items["Login"] as Login;

        await _loginService.RequestPasswordResetAsync(login!.Username, user!.Email);

        return Ok(new { message = "Password reset link has been sent to your email" });
    }

    private async Task SendCancellationConfirmationAsync(Customer customer, IptvPackage package)
    {
        var setting = await _db.WorkflowEmailSettings
            .Include(w => w.EmailTemplate)
            .FirstOrDefaultAsync(w => w.WorkflowType == "Cancellation" && w.RecipientType == "Customer");

        if (setting?.EmailTemplate == null)
            return;

        var vars = new Dictionary<string, string>
        {
            ["customer.firstName"] = customer.FirstName,
            ["customer.lastName"] = customer.LastName,
            ["customer.name"] = $"{customer.FirstName} {customer.LastName}".Trim(),
            ["customer.email"] = customer.Email,
            ["package.name"] = package.PackageName,
            ["package.price"] = package.Price.ToString("F2"),
            ["package.billingPeriod"] = package.BillingPeriod == BillingPeriod.Annual ? "Annual" : "Monthly"
        };

        var subject = $"Subscription cancelled - {package.PackageName}";
        var body = TemplateRenderer.Render(setting.EmailTemplate.BodyHtml, vars);

        try
        {
            await _smtpClient.SendEmailAsync(customer.Email, customer.Email, subject, "", body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send cancellation confirmation to {Email}", customer.Email);
        }
    }

    private async Task SendCancellationAdminNotificationAsync(Customer customer, IptvPackage package)
    {
        var setting = await _db.WorkflowEmailSettings
            .Include(w => w.EmailTemplate)
            .FirstOrDefaultAsync(w => w.WorkflowType == "Cancellation" && w.RecipientType == "Admin");

        var recipientUserIds = await _db.WorkflowNotificationRecipients
            .Where(r => r.WorkflowType == "Cancellation")
            .Select(r => r.UserId)
            .ToListAsync();

        if (recipientUserIds.Count == 0)
            return;

        var recipients = await _db.Users
            .Where(u => recipientUserIds.Contains(u.UserId) && u.Email != "")
            .ToListAsync();

        if (recipients.Count == 0)
            return;

        var vars = new Dictionary<string, string>
        {
            ["customer.firstName"] = customer.FirstName,
            ["customer.lastName"] = customer.LastName,
            ["customer.name"] = $"{customer.FirstName} {customer.LastName}".Trim(),
            ["customer.email"] = customer.Email,
            ["package.name"] = package.PackageName,
            ["package.price"] = package.Price.ToString("F2"),
            ["package.billingPeriod"] = package.BillingPeriod == BillingPeriod.Annual ? "Annual" : "Monthly"
        };

        string subject;
        string body;

        if (setting?.EmailTemplate != null)
        {
            subject = $"Subscription cancelled - {customer.FirstName} {customer.LastName} - {package.PackageName}";
            body = TemplateRenderer.Render(setting.EmailTemplate.BodyHtml, vars);
        }
        else
        {
            subject = $"Subscription cancelled - {customer.FirstName} {customer.LastName} - {package.PackageName}";
            body = $"""
                <h2>Subscription Cancellation</h2>
                <p><strong>Customer:</strong> {customer.FirstName} {customer.LastName} ({customer.Email})</p>
                <p><strong>Package:</strong> {package.PackageName}</p>
                <p><strong>Price:</strong> £{package.Price:F2} / {(package.BillingPeriod == BillingPeriod.Annual ? "Annual" : "Monthly")}</p>
                <p>This customer has cancelled their subscription and their account needs to be deactivated.</p>
                """;
        }

        foreach (var recipient in recipients)
        {
            try
            {
                await _smtpClient.SendEmailAsync(recipient.Email, recipient.Email, subject, "", body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send cancellation notification to {Email}", recipient.Email);
            }
        }
    }
}
