namespace AutoMailerBackend.Models;

public enum BillingPeriod
{
    Monthly,
    Annual
}

public class IptvPackage
{
    public int IptvPackageId { get; set; }
    public Guid IptvPackageGuid { get; set; } = Guid.NewGuid();
    public string PackageName { get; set; } = "";
    public decimal Price { get; set; }
    public BillingPeriod BillingPeriod { get; set; }
}
