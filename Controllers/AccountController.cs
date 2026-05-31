using AutoMailerBackend.Auth;
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

    public AccountController(AppDbContext db, LoginService loginService, SubscriptionsService subscriptionsService)
    {
        _db = db;
        _loginService = loginService;
        _subscriptionsService = subscriptionsService;
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
}
