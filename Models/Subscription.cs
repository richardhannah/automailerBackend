namespace AutoMailerBackend.Models;

public enum SubscriptionStatus
{
    Pending,
    Subscribed,
    Cancelled
}

public class Subscription
{
    public int SubscriptionId { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public int IptvPackageId { get; set; }
    public IptvPackage IptvPackage { get; set; } = null!;
    public DateTime DateStarted { get; set; }
    public DateTime? DateEnded { get; set; }
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Pending;
}
