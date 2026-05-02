namespace AutoMailerBackend.Models;

public class User
{
    public Guid UserId { get; set; }
    public UserRole Role { get; set; } = UserRole.User;
    public Login Login { get; set; } = null!;
}
