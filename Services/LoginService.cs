using AutoMailerBackend.Clients;
using AutoMailerBackend.Data;
using AutoMailerBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoMailerBackend.Services;

public class LoginService
{
    private readonly AppDbContext _db;
    private readonly EmailService _emailService;
    private readonly WorkflowEmailSettingsService _workflowService;
    private readonly string _frontendUrl;

    public LoginService(AppDbContext db, EmailService emailService, WorkflowEmailSettingsService workflowService, IConfiguration configuration)
    {
        _db = db;
        _emailService = emailService;
        _workflowService = workflowService;
        _frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL") ?? "http://localhost:5173";
    }

    public async Task<LoginResponse?> LoginAsync(string username, string password)
    {
        var login = await _db.Logins.Include(l => l.User).FirstOrDefaultAsync(l => l.Username == username);

        if (login == null || !PasswordHasher.Verify(password, login.Salt, login.Password))
            return null;

        if (!login.User.EmailVerified)
            return new LoginResponse
            {
                Token = Guid.Empty,
                Username = login.Username,
                Role = login.User.Role.ToString(),
                EmailVerified = false
            };

        login.Token = Guid.NewGuid();
        await _db.SaveChangesAsync();

        return new LoginResponse
        {
            Token = login.Token,
            Username = login.Username,
            Role = login.User.Role.ToString(),
            EmailVerified = true
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
        var verificationToken = Guid.NewGuid();

        var newUser = new User
        {
            UserId = userId,
            Role = UserRole.User,
            Email = email,
            EmailVerified = false,
            EmailVerificationToken = verificationToken
        };
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
            Notes = "New customer/prospect - registered via sign-up",
            UserId = userId
        };

        _db.Users.Add(newUser);
        _db.Logins.Add(newLogin);
        _db.Customers.Add(newCustomer);
        await _db.SaveChangesAsync();

        // Send verification email using workflow template
        await SendVerificationEmailAsync(email, username, verificationToken);

        return new RegisterResult
        {
            Success = true,
            Response = new LoginResponse
            {
                Token = Guid.Empty,
                Username = newLogin.Username,
                Role = newUser.Role.ToString(),
                EmailVerified = false
            }
        };
    }

    public async Task<bool> VerifyEmailAsync(Guid token)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.EmailVerificationToken == token);
        if (user == null)
            return false;

        if (!user.EmailVerified)
        {
            user.EmailVerified = true;
            await _db.SaveChangesAsync();
        }

        return true;
    }

    public async Task<bool> ResendVerificationAsync(string username)
    {
        var login = await _db.Logins.Include(l => l.User).FirstOrDefaultAsync(l => l.Username == username);
        if (login == null || login.User.EmailVerified)
            return false;

        var newToken = Guid.NewGuid();
        login.User.EmailVerificationToken = newToken;
        await _db.SaveChangesAsync();

        await SendVerificationEmailAsync(login.User.Email, login.Username, newToken);
        return true;
    }

    private async Task SendVerificationEmailAsync(string email, string username, Guid token)
    {
        var verifyUrl = $"{_frontendUrl}/verify-email?token={token}";

        var workflowSetting = await _workflowService.GetAsync("Registration", "User");
        if (workflowSetting?.EmailTemplate != null)
        {
            var template = workflowSetting.EmailTemplate;
            var vars = new Dictionary<string, string>
            {
                ["user.username"] = username,
                ["user.email"] = email,
                ["verificationLink"] = verifyUrl
            };

            var body = !string.IsNullOrEmpty(template.BodyHtml)
                ? TemplateRenderer.Render(template.BodyHtml, vars)
                : TemplateRenderer.Render(template.BodyText, vars);

            var subject = "Verify your email address";

            await _emailService.SendEmailAsync(new SendEmailRequest
            {
                To = email,
                ToName = username,
                Subject = subject,
                Body = body
            });
        }
        else
        {
            // Fallback: send a plain text verification email
            var body = $"Hi {username},\n\nPlease verify your email address by clicking the link below:\n\n{verifyUrl}\n\nIf you didn't create an account, you can ignore this email.";

            await _emailService.SendEmailAsync(new SendEmailRequest
            {
                To = email,
                ToName = username,
                Subject = "Verify your email address",
                Body = body
            });
        }
    }
}

public class LoginResponse
{
    public Guid Token { get; set; }
    public string Username { get; set; } = "";
    public string Role { get; set; } = "";
    public bool EmailVerified { get; set; } = true;
}

public class RegisterResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public LoginResponse? Response { get; set; }
}
