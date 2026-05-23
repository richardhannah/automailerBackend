using AutoMailerBackend.Clients;
using AutoMailerBackend.Data;
using AutoMailerBackend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutoMailerBackend.Services;

public class SubscriptionsService
{
    private readonly AppDbContext _db;
    private readonly Smtp2GoClient _smtpClient;
    private readonly ILogger<SubscriptionsService> _logger;

    public SubscriptionsService(AppDbContext db, Smtp2GoClient smtpClient, ILogger<SubscriptionsService> logger)
    {
        _db = db;
        _smtpClient = smtpClient;
        _logger = logger;
    }

    public async Task<List<Subscription>> GetAllAsync()
    {
        return await _db.Subscriptions
            .Include(s => s.Customer)
            .Include(s => s.IptvPackage)
            .OrderByDescending(s => s.DateStarted)
            .ToListAsync();
    }

    public async Task<List<Subscription>> GetByCustomerIdAsync(int customerId)
    {
        return await _db.Subscriptions
            .Include(s => s.Customer)
            .Include(s => s.IptvPackage)
            .Where(s => s.CustomerId == customerId)
            .OrderByDescending(s => s.DateStarted)
            .ToListAsync();
    }

    public async Task<Subscription?> GetByIdAsync(int id)
    {
        return await _db.Subscriptions
            .Include(s => s.Customer)
            .Include(s => s.IptvPackage)
            .FirstOrDefaultAsync(s => s.SubscriptionId == id);
    }

    public async Task<SubscriptionCreateResult> CreateAsync(int customerId, int iptvPackageId, string? status)
    {
        var customerExists = await _db.Customers.AnyAsync(c => c.CustomerId == customerId);
        if (!customerExists)
            return new SubscriptionCreateResult { Success = false, Error = "Customer not found" };

        var packageExists = await _db.IptvPackages.AnyAsync(p => p.IptvPackageId == iptvPackageId);
        if (!packageExists)
            return new SubscriptionCreateResult { Success = false, Error = "Package not found" };

        var parsedStatus = SubscriptionStatus.Pending;
        if (status != null && !Enum.TryParse(status, true, out parsedStatus))
            return new SubscriptionCreateResult { Success = false, Error = "Invalid status" };

        var subscription = new Subscription
        {
            CustomerId = customerId,
            IptvPackageId = iptvPackageId,
            DateStarted = DateTime.UtcNow,
            Status = parsedStatus
        };

        _db.Subscriptions.Add(subscription);
        await _db.SaveChangesAsync();

        subscription = await GetByIdAsync(subscription.SubscriptionId);
        return new SubscriptionCreateResult { Success = true, Subscription = subscription };
    }

    public async Task<SubscriptionCreateResult> SubscribeAsync(Guid userId, int iptvPackageId)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
        if (customer == null)
            return new SubscriptionCreateResult { Success = false, Error = "No customer record linked to this user" };

        var package = await _db.IptvPackages.FindAsync(iptvPackageId);
        if (package == null)
            return new SubscriptionCreateResult { Success = false, Error = "Package not found" };

        var alreadySubscribed = await _db.Subscriptions
            .AnyAsync(s => s.CustomerId == customer.CustomerId
                        && s.IptvPackageId == iptvPackageId
                        && s.Status != SubscriptionStatus.Cancelled);
        if (alreadySubscribed)
            return new SubscriptionCreateResult { Success = false, Error = "Already subscribed to this package" };

        var subscription = new Subscription
        {
            CustomerId = customer.CustomerId,
            IptvPackageId = iptvPackageId,
            DateStarted = DateTime.UtcNow,
            Status = SubscriptionStatus.Pending
        };

        _db.Subscriptions.Add(subscription);
        await _db.SaveChangesAsync();

        // Send confirmation email using Subscription workflow template
        await SendSubscriptionEmailAsync(customer, package);

        subscription = await GetByIdAsync(subscription.SubscriptionId);
        return new SubscriptionCreateResult { Success = true, Subscription = subscription };
    }

    private async Task SendSubscriptionEmailAsync(Customer customer, IptvPackage package)
    {
        var setting = await _db.WorkflowEmailSettings
            .Include(w => w.EmailTemplate)
            .FirstOrDefaultAsync(w => w.WorkflowType == "Subscription" && w.RecipientType == "Customer");

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

        var subject = $"Subscription request received - {package.PackageName}";
        var body = TemplateRenderer.Render(setting.EmailTemplate.BodyHtml, vars);

        try
        {
            await _smtpClient.SendEmailAsync(customer.Email, customer.Email, subject, "", body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send subscription confirmation to {Email}", customer.Email);
        }
    }

    public async Task<SubscriptionUpdateResult> UpdateAsync(int id, int? iptvPackageId, string? status, DateTime? dateEnded)
    {
        var subscription = await _db.Subscriptions.FindAsync(id);
        if (subscription == null)
            return new SubscriptionUpdateResult { Found = false };

        if (iptvPackageId.HasValue)
        {
            var packageExists = await _db.IptvPackages.AnyAsync(p => p.IptvPackageId == iptvPackageId.Value);
            if (!packageExists)
                return new SubscriptionUpdateResult { Found = true, Success = false, Error = "Package not found" };
            subscription.IptvPackageId = iptvPackageId.Value;
        }

        if (status != null)
        {
            if (!Enum.TryParse<SubscriptionStatus>(status, true, out var parsedStatus))
                return new SubscriptionUpdateResult { Found = true, Success = false, Error = "Invalid status" };
            subscription.Status = parsedStatus;
        }

        if (dateEnded.HasValue)
            subscription.DateEnded = DateTime.SpecifyKind(dateEnded.Value, DateTimeKind.Utc);

        await _db.SaveChangesAsync();

        var updated = await GetByIdAsync(id);
        return new SubscriptionUpdateResult { Found = true, Success = true, Subscription = updated };
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var subscription = await _db.Subscriptions.FindAsync(id);
        if (subscription == null)
            return false;

        _db.Subscriptions.Remove(subscription);
        await _db.SaveChangesAsync();
        return true;
    }
}

public class SubscriptionCreateResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public Subscription? Subscription { get; set; }
}

public class SubscriptionUpdateResult
{
    public bool Found { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
    public Subscription? Subscription { get; set; }
}
