using AutoMailerBackend.Data;
using AutoMailerBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoMailerBackend.Services;

public class SubscriptionsService
{
    private readonly AppDbContext _db;

    public SubscriptionsService(AppDbContext db)
    {
        _db = db;
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
