using AutoMailerBackend.Auth;
using AutoMailerBackend.Models;
using AutoMailerBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoMailerBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoginController : ControllerBase
{
    private readonly LoginService _loginService;

    public LoginController(LoginService loginService)
    {
        _loginService = loginService;
    }

    [HttpPost]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _loginService.LoginAsync(request.Username, request.Password);

        if (result == null)
            return Unauthorized(new { error = "Invalid credentials" });

        return Ok(result);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _loginService.RegisterAsync(request.Username, request.Password, request.Email, request.Phone);

        if (!result.Success)
            return Conflict(new { error = result.Error });

        return Ok(result.Response);
    }

    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromQuery] Guid token)
    {
        var success = await _loginService.VerifyEmailAsync(token);

        if (!success)
            return BadRequest(new { error = "Invalid or expired verification link" });

        return Ok(new { message = "Email verified successfully" });
    }

    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationRequest request)
    {
        await _loginService.ResendVerificationAsync(request.Username);
        // Always return OK to avoid leaking whether the username exists
        return Ok(new { message = "If the account exists and is unverified, a new verification email has been sent" });
    }

    [HttpGet("me")]
    [TokenAuth]
    public IActionResult Me()
    {
        var user = HttpContext.Items["User"] as User;
        var login = HttpContext.Items["Login"] as Login;

        return Ok(new
        {
            username = login!.Username,
            role = user!.Role.ToString()
        });
    }
}

public class ResendVerificationRequest
{
    public required string Username { get; set; }
}

public class LoginRequest
{
    public required string Username { get; set; }
    public required string Password { get; set; }
}

public class RegisterRequest
{
    public required string Username { get; set; }
    public required string Password { get; set; }
    public required string Email { get; set; }
    public string? Phone { get; set; }
}
