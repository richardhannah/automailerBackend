namespace AutoMailerBackend.Models;

public class EmailTemplate
{
    public int EmailTemplateId { get; set; }
    public Guid EmailTemplateGuid { get; set; } = Guid.NewGuid();
    public string TemplateName { get; set; } = "";
    public string BodyText { get; set; } = "";
    public string BodyHtml { get; set; } = "";
}
