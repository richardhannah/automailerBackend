using AutoMailerBackend.Auth;
using AutoMailerBackend.Data;
using AutoMailerBackend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoMailerBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[TokenAuth]
[RequireRole(UserRole.Admin)]
public class CustomersController : ControllerBase
{
    private readonly AppDbContext _db;

    public CustomersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var customers = await _db.Customers.ToListAsync();
        return Ok(customers);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var customer = await _db.Customers.FindAsync(id);
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

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = customer.CustomerId }, customer);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerRequest request)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer == null)
            return NotFound(new { error = "Customer not found" });

        customer.FirstName = request.FirstName;
        customer.LastName = request.LastName;
        customer.Email = request.Email;
        customer.Phone = request.Phone;
        customer.IptvUser = request.IptvUser;
        customer.IptvPassword = request.IptvPassword;
        customer.Notes = request.Notes;
        customer.ExpirationDate = request.ExpirationDate;
        customer.FollowUp = request.FollowUp;

        await _db.SaveChangesAsync();
        return Ok(customer);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer == null)
            return NotFound(new { error = "Customer not found" });

        _db.Customers.Remove(customer);
        await _db.SaveChangesAsync();

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
