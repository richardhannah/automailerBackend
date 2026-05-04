namespace AutoMailerBackend.Models;

public class Enquiry
{
    public int EnquiryId { get; set; }
    public string Email { get; set; } = "";
    public string? PhoneNumber { get; set; }
    public DateTime DateReceived { get; set; } = DateTime.UtcNow;
    public string Message { get; set; } = "";
}
