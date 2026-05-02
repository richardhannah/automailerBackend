using AutoMailerBackend.Data;
using AutoMailerBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoMailerBackend.Services;

public class LoginService
{
    private readonly AppDbContext _db;

    public LoginService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<LoginResponse?> LoginOrRegisterAsync(string username, string password)
    {
        var login = await _db.Logins.Include(l => l.User).FirstOrDefaultAsync(l => l.Username == username);

        if (login != null)
        {
            if (!PasswordHasher.Verify(password, login.Salt, login.Password))
                return null;

            login.Token = Guid.NewGuid();
            await _db.SaveChangesAsync();

            return new LoginResponse
            {
                Token = login.Token,
                Username = login.Username,
                Role = login.User.Role.ToString()
            };
        }

        var salt = PasswordHasher.GenerateSalt();
        var hash = PasswordHasher.Hash(password, salt);
        var userId = Guid.NewGuid();

        var newUser = new User { UserId = userId, Role = UserRole.User };
        var newLogin = new Login
        {
            UserId = userId,
            Username = username,
            Password = hash,
            Salt = salt,
            Token = Guid.NewGuid()
        };

        _db.Users.Add(newUser);
        _db.Logins.Add(newLogin);
        await _db.SaveChangesAsync();

        return new LoginResponse
        {
            Token = newLogin.Token,
            Username = newLogin.Username,
            Role = newUser.Role.ToString()
        };
    }
}

public class LoginResponse
{
    public Guid Token { get; set; }
    public string Username { get; set; } = "";
    public string Role { get; set; } = "";
}
