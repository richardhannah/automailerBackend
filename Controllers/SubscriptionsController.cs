using AutoMailerBackend.Auth;
using AutoMailerBackend.Models;
using AutoMailerBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoMailerBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[TokenAuth]
[RequireRole(UserRole.Admin)]
public class SubscriptionsController : ControllerBase
{
    private readonly SubscriptionsService _service;

    public SubscriptionsController(SubscriptionsService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var subscriptions = await _service.GetAllAsync();

        var result = subscriptions.Select(s => new
        {
            s.SubscriptionId,
            s.CustomerId,
            CustomerName = $"{s.Customer.FirstName} {s.Customer.LastName}".Trim(),
            CustomerEmail = s.Customer.Email,
            s.IptvPackageId,
            PackageName = s.IptvPackage.PackageName,
            s.DateStarted,
            s.DateEnded,
            Status = s.Status.ToString(),
            Links = new
            {
                Self = Url.Action(nameof(GetById), new { id = s.SubscriptionId }),
                Update = Url.Action(nameof(Update), new { id = s.SubscriptionId }),
                Delete = Url.Action(nameof(Delete), new { id = s.SubscriptionId })
            }
        });

        return Ok(result);
    }

    [HttpGet("customer/{customerId}")]
    public async Task<IActionResult> GetByCustomer(int customerId)
    {
        var subscriptions = await _service.GetByCustomerIdAsync(customerId);

        var result = subscriptions.Select(s => new
        {
            s.SubscriptionId,
            s.CustomerId,
            s.IptvPackageId,
            PackageName = s.IptvPackage.PackageName,
            s.DateStarted,
            s.DateEnded,
            Status = s.Status.ToString(),
            Links = new
            {
                Self = Url.Action(nameof(GetById), new { id = s.SubscriptionId }),
                Update = Url.Action(nameof(Update), new { id = s.SubscriptionId }),
                Delete = Url.Action(nameof(Delete), new { id = s.SubscriptionId })
            }
        });

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var s = await _service.GetByIdAsync(id);

        if (s == null)
            return NotFound(new { error = "Subscription not found" });

        return Ok(new
        {
            s.SubscriptionId,
            s.CustomerId,
            CustomerName = $"{s.Customer.FirstName} {s.Customer.LastName}".Trim(),
            CustomerEmail = s.Customer.Email,
            s.IptvPackageId,
            PackageName = s.IptvPackage.PackageName,
            s.DateStarted,
            s.DateEnded,
            Status = s.Status.ToString(),
            Links = new
            {
                Self = Url.Action(nameof(GetById), new { id = s.SubscriptionId }),
                Update = Url.Action(nameof(Update), new { id = s.SubscriptionId }),
                Delete = Url.Action(nameof(Delete), new { id = s.SubscriptionId })
            }
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSubscriptionRequest request)
    {
        var result = await _service.CreateAsync(request.CustomerId, request.IptvPackageId, request.Status);

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        var s = result.Subscription!;
        return CreatedAtAction(nameof(GetById), new { id = s.SubscriptionId }, new
        {
            s.SubscriptionId,
            s.CustomerId,
            CustomerName = $"{s.Customer.FirstName} {s.Customer.LastName}".Trim(),
            s.IptvPackageId,
            PackageName = s.IptvPackage.PackageName,
            s.DateStarted,
            s.DateEnded,
            Status = s.Status.ToString(),
            Links = new
            {
                Self = Url.Action(nameof(GetById), new { id = s.SubscriptionId }),
                Update = Url.Action(nameof(Update), new { id = s.SubscriptionId }),
                Delete = Url.Action(nameof(Delete), new { id = s.SubscriptionId })
            }
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSubscriptionRequest request)
    {
        var result = await _service.UpdateAsync(id, request.IptvPackageId, request.Status, request.DateEnded);

        if (!result.Found)
            return NotFound(new { error = "Subscription not found" });

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        var s = result.Subscription!;
        return Ok(new
        {
            s.SubscriptionId,
            s.CustomerId,
            CustomerName = $"{s.Customer.FirstName} {s.Customer.LastName}".Trim(),
            s.IptvPackageId,
            PackageName = s.IptvPackage.PackageName,
            s.DateStarted,
            s.DateEnded,
            Status = s.Status.ToString(),
            Links = new
            {
                Self = Url.Action(nameof(GetById), new { id = s.SubscriptionId }),
                Update = Url.Action(nameof(Update), new { id = s.SubscriptionId }),
                Delete = Url.Action(nameof(Delete), new { id = s.SubscriptionId })
            }
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id);

        if (!deleted)
            return NotFound(new { error = "Subscription not found" });

        return Ok(new { message = "Subscription deleted" });
    }
}

public class CreateSubscriptionRequest
{
    public required int CustomerId { get; set; }
    public required int IptvPackageId { get; set; }
    public string? Status { get; set; }
}

public class UpdateSubscriptionRequest
{
    public int? IptvPackageId { get; set; }
    public string? Status { get; set; }
    public DateTime? DateEnded { get; set; }
}
