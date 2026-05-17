using AutoMailerBackend.Auth;
using AutoMailerBackend.Models;
using AutoMailerBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoMailerBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[TokenAuth]
[RequireRole(UserRole.Admin)]
public class CustomersController : ControllerBase
{
    private readonly CustomersService _service;

    public CustomersController(CustomersService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var customers = await _service.GetAllAsync();
        return Ok(customers);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var customer = await _service.GetByIdAsync(id);
        if (customer == null)
            return NotFound(new { error = "Customer not found" });

        return Ok(customer);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequest request)
    {
        var customer = new Customer
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            IptvUser = request.IptvUser,
            IptvPassword = request.IptvPassword,
            Notes = request.Notes,
            ExpirationDate = request.ExpirationDate,
            FollowUp = request.FollowUp
        };

        await _service.CreateAsync(customer);

        return CreatedAtAction(nameof(GetById), new { id = customer.CustomerId }, customer);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerRequest request)
    {
        var customer = await _service.UpdateAsync(id, c =>
        {
            c.FirstName = request.FirstName;
            c.LastName = request.LastName;
            c.Email = request.Email;
            c.Phone = request.Phone;
            c.IptvUser = request.IptvUser;
            c.IptvPassword = request.IptvPassword;
            c.Notes = request.Notes;
            c.ExpirationDate = request.ExpirationDate;
            c.FollowUp = request.FollowUp;
        });

        if (customer == null)
            return NotFound(new { error = "Customer not found" });

        return Ok(customer);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id);

        if (!deleted)
            return NotFound(new { error = "Customer not found" });

        return Ok(new { message = "Customer deleted" });
    }
}

public class CreateCustomerRequest
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public string Phone { get; set; } = "";
    public required string IptvUser { get; set; }
    public string IptvPassword { get; set; } = "";
    public string Notes { get; set; } = "";
    public DateOnly? ExpirationDate { get; set; }
    public bool FollowUp { get; set; }
}

public class UpdateCustomerRequest
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public string Phone { get; set; } = "";
    public required string IptvUser { get; set; }
    public string IptvPassword { get; set; } = "";
    public string Notes { get; set; } = "";
    public DateOnly? ExpirationDate { get; set; }
    public bool FollowUp { get; set; }
}
