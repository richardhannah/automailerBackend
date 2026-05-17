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
    public async Task<IActionResult> Register([FromBody] LoginRequest request)
    {
        var result = await _loginService.RegisterAsync(request.Username, request.Password);

        if (!result.Success)
            return Conflict(new { error = result.Error });

        return Ok(result.Response);
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

public class LoginRequest
{
    public required string Username { get; set; }
    public required string Password { get; set; }
}
