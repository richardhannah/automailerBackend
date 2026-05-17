using AutoMailerBackend.Data;
using AutoMailerBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoMailerBackend.Services;

public class IptvPackagesService
{
    private readonly AppDbContext _db;

    public IptvPackagesService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<IptvPackage>> GetAllAsync()
    {
        return await _db.IptvPackages.ToListAsync();
    }

    public async Task<IptvPackage?> GetByGuidAsync(Guid guid)
    {
        return await _db.IptvPackages
            .FirstOrDefaultAsync(p => p.IptvPackageGuid == guid);
    }

    public async Task<IptvPackage> CreateAsync(IptvPackage package)
    {
        _db.IptvPackages.Add(package);
        await _db.SaveChangesAsync();
        return package;
    }

    public async Task<IptvPackage?> UpdateAsync(Guid guid, string packageName, decimal price, BillingPeriod billingPeriod)
    {
        var package = await _db.IptvPackages
            .FirstOrDefaultAsync(p => p.IptvPackageGuid == guid);

        if (package == null)
            return null;

        package.PackageName = packageName;
        package.Price = price;
        package.BillingPeriod = billingPeriod;

        await _db.SaveChangesAsync();
        return package;
    }

    public async Task<bool> DeleteAsync(Guid guid)
    {
        var package = await _db.IptvPackages
            .FirstOrDefaultAsync(p => p.IptvPackageGuid == guid);

        if (package == null)
            return false;

        _db.IptvPackages.Remove(package);
        await _db.SaveChangesAsync();
        return true;
    }
}
