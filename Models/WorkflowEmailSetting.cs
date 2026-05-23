namespace AutoMailerBackend.Models;

public class WorkflowEmailSetting
{
    public int WorkflowEmailSettingId { get; set; }
    public string WorkflowType { get; set; } = "";
    public string RecipientType { get; set; } = "";
    public int? EmailTemplateId { get; set; }
    public EmailTemplate? EmailTemplate { get; set; }
}
