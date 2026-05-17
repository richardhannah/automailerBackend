using AutoMailerBackend.Data;
using AutoMailerBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoMailerBackend.Services;

public class UsersService
{
    private readonly AppDbContext _db;

    public UsersService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<UserDto>> GetAllAsync()
    {
        return await _db.Users
            .Include(u => u.Login)
            .Select(u => new UserDto
            {
                UserId = u.UserId,
                Username = u.Login.Username,
                Role = u.Role.ToString()
            })
            .ToListAsync();
    }

    public async Task<UserRoleResult?> UpdateRoleAsync(Guid userId, string roleName)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null)
            return null;

        if (!Enum.TryParse<UserRole>(roleName, true, out var role))
            return new UserRoleResult { Success = false, Error = "Invalid role" };

        user.Role = role;
        await _db.SaveChangesAsync();

        return new UserRoleResult
        {
            Success = true,
            UserId = user.UserId,
            Role = user.Role.ToString()
        };
    }

    public async Task<bool> DeleteAsync(Guid userId)
    {
        var user = await _db.Users.Include(u => u.Login).FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null)
            return false;

        _db.Logins.Remove(user.Login);
        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return true;
    }
}

public class UserDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = "";
    public string Role { get; set; } = "";
}

public class UserRoleResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public Guid UserId { get; set; }
    public string Role { get; set; } = "";
}
