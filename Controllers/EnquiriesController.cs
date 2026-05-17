using AutoMailerBackend.Auth;
using AutoMailerBackend.Models;
using AutoMailerBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoMailerBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EnquiriesController : ControllerBase
{
    private readonly EnquiriesService _enquiriesService;

    public EnquiriesController(EnquiriesService enquiriesService)
    {
        _enquiriesService = enquiriesService;
    }

    [HttpGet]
    [TokenAuth]
    [RequireRole(UserRole.Admin)]
    public async Task<IActionResult> GetAll()
    {
        var enquiries = await _enquiriesService.GetAllAsync();
        return Ok(enquiries);
    }

    [HttpGet("{id}")]
    [TokenAuth]
    [RequireRole(UserRole.Admin)]
    public async Task<IActionResult> GetById(int id)
    {
        var enquiry = await _enquiriesService.GetByIdAsync(id);
        if (enquiry == null)
            return NotFound(new { error = "Enquiry not found" });

        return Ok(enquiry);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEnquiryRequest request)
    {
        var enquiry = await _enquiriesService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = enquiry.EnquiryId }, enquiry);
    }

    [HttpDelete("{id}")]
    [TokenAuth]
    [RequireRole(UserRole.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _enquiriesService.DeleteAsync(id);
        if (!deleted)
            return NotFound(new { error = "Enquiry not found" });

        return Ok(new { message = "Enquiry deleted" });
    }
}

public class CreateEnquiryRequest
{
    public required string Email { get; set; }
    public string? PhoneNumber { get; set; }
    public required string Message { get; set; }
}
