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

    public async Task<LoginResponse?> LoginAsync(string username, string password)
    {
        var login = await _db.Logins.Include(l => l.User).FirstOrDefaultAsync(l => l.Username == username);

        if (login == null || !PasswordHasher.Verify(password, login.Salt, login.Password))
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

    public async Task<RegisterResult> RegisterAsync(string username, string password, string email, string? phone)
    {
        var exists = await _db.Logins.AnyAsync(l => l.Username == username);
        if (exists)
            return new RegisterResult { Success = false, Error = "Username already taken" };

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

        var newCustomer = new Customer
        {
            FirstName = username,
            Email = email,
            Phone = phone ?? "",
            Notes = "New customer/prospect - registered via sign-up"
        };

        _db.Users.Add(newUser);
        _db.Logins.Add(newLogin);
        _db.Customers.Add(newCustomer);
        await _db.SaveChangesAsync();

        return new RegisterResult
        {
            Success = true,
            Response = new LoginResponse
            {
                Token = newLogin.Token,
                Username = newLogin.Username,
                Role = newUser.Role.ToString()
            }
        };
    }
}

public class LoginResponse
{
    public Guid Token { get; set; }
    public string Username { get; set; } = "";
    public string Role { get; set; } = "";
}

public class RegisterResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public LoginResponse? Response { get; set; }
}
