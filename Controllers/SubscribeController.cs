using AutoMailerBackend.Auth;
using AutoMailerBackend.Models;
using AutoMailerBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoMailerBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[TokenAuth]
public class SubscribeController : ControllerBase
{
    private readonly SubscriptionsService _service;

    public SubscribeController(SubscriptionsService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest request)
    {
        var currentUser = HttpContext.Items["User"] as User;
        if (currentUser == null)
            return Unauthorized();

        var result = await _service.SubscribeAsync(currentUser.UserId, request.IptvPackageId);

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        var s = result.Subscription!;
        return Ok(new
        {
            s.SubscriptionId,
            s.IptvPackageId,
            PackageName = s.IptvPackage.PackageName,
            Status = s.Status.ToString()
        });
    }
}

public class SubscribeRequest
{
    public required int IptvPackageId { get; set; }
}
