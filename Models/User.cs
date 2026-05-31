namespace AutoMailerBackend.Models;

public class User
{
    public Guid UserId { get; set; }
    public UserRole Role { get; set; } = UserRole.User;
    public string Email { get; set; } = "";
    public bool EmailVerified { get; set; } = false;
    public Guid? EmailVerificationToken { get; set; }
    public Login Login { get; set; } = null!;
}
