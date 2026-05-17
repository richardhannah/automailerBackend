using AutoMailerBackend.Auth;
using AutoMailerBackend.Models;
using AutoMailerBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoMailerBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[TokenAuth]
[RequireRole(UserRole.Admin)]
public class UsersController : ControllerBase
{
    private readonly UsersService _service;

    public UsersController(UsersService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _service.GetAllAsync();
        return Ok(users);
    }

    [HttpPut("{userId}/role")]
    public async Task<IActionResult> UpdateRole(Guid userId, [FromBody] UpdateRoleRequest request)
    {
        var result = await _service.UpdateRoleAsync(userId, request.Role);

        if (result == null)
            return NotFound(new { error = "User not found" });

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(new { userId = result.UserId, role = result.Role });
    }

    [HttpDelete("{userId}")]
    public async Task<IActionResult> Delete(Guid userId)
    {
        var deleted = await _service.DeleteAsync(userId);

        if (!deleted)
            return NotFound(new { error = "User not found" });

        return Ok(new { message = "User deleted" });
    }
}

public class UpdateRoleRequest
{
    public required string Role { get; set; }
}
