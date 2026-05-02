namespace AutoMailerBackend.Models;

public class Login
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string Salt { get; set; } = "";
    public Guid Token { get; set; }
    public User User { get; set; } = null!;
}
