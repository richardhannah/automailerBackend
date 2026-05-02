namespace AutoMailerBackend.Models;

public class SendEmailRequest
{
    public required string To { get; set; }
    public required string ToName { get; set; }
    public required string Subject { get; set; }
    public required string Body { get; set; }
}
