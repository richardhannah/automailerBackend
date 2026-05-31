using AutoMailerBackend.Auth;
using AutoMailerBackend.Models;
using AutoMailerBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoMailerBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SocialMediaLinksController : ControllerBase
{
    private readonly SocialMediaLinksService _service;

    public SocialMediaLinksController(SocialMediaLinksService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var links = await _service.GetAllAsync();
        return Ok(links.Select(l => new { l.Platform, l.Url }));
    }

    [HttpPut]
    [TokenAuth]
    [RequireRole(UserRole.Admin)]
    public async Task<IActionResult> Save([FromBody] List<SocialMediaLinkDto> links)
    {
        await _service.SaveAsync(links);
        return Ok(new { message = "Social media links saved" });
    }
}
