namespace AutoMailerBackend.Models;

public class Customer
{
    public int CustomerId { get; set; }
    public Guid CustomerGuid { get; set; } = Guid.NewGuid();
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string IptvUser { get; set; } = "";
    public string IptvPassword { get; set; } = "";
    public string Notes { get; set; } = "";
    public DateOnly? ExpirationDate { get; set; }
    public bool FollowUp { get; set; }
}
