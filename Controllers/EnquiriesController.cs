using AutoMailerBackend.Auth;
using AutoMailerBackend.Data;
using AutoMailerBackend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoMailerBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EnquiriesController : ControllerBase
{
    private readonly AppDbContext _db;

    public EnquiriesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [TokenAuth]
    [RequireRole(UserRole.Admin)]
    public async Task<IActionResult> GetAll()
    {
        var enquiries = await _db.Enquiries.OrderByDescending(e => e.DateReceived).ToListAsync();
        return Ok(enquiries);
    }

    [HttpGet("{id}")]
    [TokenAuth]
    [RequireRole(UserRole.Admin)]
    public async Task<IActionResult> GetById(int id)
    {
        var enquiry = await _db.Enquiries.FindAsync(id);
        if (enquiry == null)
            return NotFound(new { error = "Enquiry not found" });

        return Ok(enquiry);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEnquiryRequest request)
    {
        var enquiry = new Enquiry
        {
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            DateReceived = DateTime.UtcNow,
            Message = request.Message
        };

        _db.Enquiries.Add(enquiry);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = enquiry.EnquiryId }, enquiry);
    }

    [HttpDelete("{id}")]
    [TokenAuth]
    [RequireRole(UserRole.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var enquiry = await _db.Enquiries.FindAsync(id);
        if (enquiry == null)
            return NotFound(new { error = "Enquiry not found" });

        _db.Enquiries.Remove(enquiry);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Enquiry deleted" });
    }
}

public class CreateEnquiryRequest
{
    public required string Email { get; set; }
    public string? PhoneNumber { get; set; }
    public required string Message { get; set; }
}
