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
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _db.Users
            .Include(u => u.Login)
            .Select(u => new
            {
                u.UserId,
                u.Login.Username,
                Role = u.Role.ToString()
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpPut("{userId}/role")]
    public async Task<IActionResult> UpdateRole(Guid userId, [FromBody] UpdateRoleRequest request)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null)
            return NotFound(new { error = "User not found" });

        if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
            return BadRequest(new { error = "Invalid role" });

        user.Role = role;
        await _db.SaveChangesAsync();

        return Ok(new { userId = user.UserId, role = user.Role.ToString() });
    }

    [HttpDelete("{userId}")]
    public async Task<IActionResult> Delete(Guid userId)
    {
        var user = await _db.Users.Include(u => u.Login).FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null)
            return NotFound(new { error = "User not found" });

        _db.Logins.Remove(user.Login);
        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        return Ok(new { message = "User deleted" });
    }
}

public class UpdateRoleRequest
{
    public required string Role { get; set; }
}
