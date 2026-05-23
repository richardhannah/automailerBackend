namespace AutoMailerBackend.Models;

public class WorkflowNotificationRecipient
{
    public int WorkflowNotificationRecipientId { get; set; }
    public string WorkflowType { get; set; } = "";
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}
