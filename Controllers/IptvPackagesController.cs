using AutoMailerBackend.Auth;
using AutoMailerBackend.Data;
using AutoMailerBackend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoMailerBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[TokenAuth]
public class IptvPackagesController : ControllerBase
{
    private readonly AppDbContext _db;

    public IptvPackagesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var packages = await _db.IptvPackages.ToListAsync();

        var result = packages.Select(p => new
        {
            p.IptvPackageId,
            p.IptvPackageGuid,
            p.PackageName,
            p.Price,
            p.BillingPeriod,
            Links = new
            {
                Self = Url.Action(nameof(GetByGuid), new { guid = p.IptvPackageGuid }),
                Update = Url.Action(nameof(Update), new { guid = p.IptvPackageGuid }),
                Delete = Url.Action(nameof(Delete), new { guid = p.IptvPackageGuid })
            }
        });

        return Ok(result);
    }

    [HttpGet("{guid}")]
    public async Task<IActionResult> GetByGuid(Guid guid)
    {
        var package = await _db.IptvPackages
            .FirstOrDefaultAsync(p => p.IptvPackageGuid == guid);

        if (package == null)
            return NotFound(new { error = "Package not found" });

        return Ok(package);
    }

    [HttpPost]
    [RequireRole(UserRole.Admin)]
    public async Task<IActionResult> Create([FromBody] CreateIptvPackageRequest request)
    {
        var package = new IptvPackage
        {
            PackageName = request.PackageName,
            Price = request.Price,
            BillingPeriod = request.BillingPeriod
        };

        _db.IptvPackages.Add(package);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetByGuid), new { guid = package.IptvPackageGuid }, package);
    }

    [HttpPut("{guid}")]
    [RequireRole(UserRole.Admin)]
    public async Task<IActionResult> Update(Guid guid, [FromBody] UpdateIptvPackageRequest request)
    {
        var package = await _db.IptvPackages
            .FirstOrDefaultAsync(p => p.IptvPackageGuid == guid);

        if (package == null)
            return NotFound(new { error = "Package not found" });

        package.PackageName = request.PackageName;
        package.Price = request.Price;
        package.BillingPeriod = request.BillingPeriod;

        await _db.SaveChangesAsync();
        return Ok(package);
    }

    [HttpDelete("{guid}")]
    [RequireRole(UserRole.Admin)]
    public async Task<IActionResult> Delete(Guid guid)
    {
        var package = await _db.IptvPackages
            .FirstOrDefaultAsync(p => p.IptvPackageGuid == guid);

        if (package == null)
            return NotFound(new { error = "Package not found" });

        _db.IptvPackages.Remove(package);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Package deleted" });
    }
}

public class CreateIptvPackageRequest
{
    public required string PackageName { get; set; }
    public decimal Price { get; set; }
    public BillingPeriod BillingPeriod { get; set; }
}

public class UpdateIptvPackageRequest
{
    public required string PackageName { get; set; }
    public decimal Price { get; set; }
    public BillingPeriod BillingPeriod { get; set; }
}
