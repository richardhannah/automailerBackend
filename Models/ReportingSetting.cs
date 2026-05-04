namespace AutoMailerBackend.Models;

public class ReportingSetting
{
    public int ReportingSettingId { get; set; }
    public string Name { get; set; } = "";
    public string EmailAddress { get; set; } = "";
    public int? EmailTemplateId { get; set; }
    public EmailTemplate? EmailTemplate { get; set; }
}
