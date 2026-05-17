using AutoMailerBackend.Auth;
using AutoMailerBackend.Models;
using AutoMailerBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoMailerBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[TokenAuth]
[RequireRole(UserRole.Admin)]
public class EmailTemplatesController : ControllerBase
{
    private readonly EmailTemplatesService _service;

    public EmailTemplatesController(EmailTemplatesService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var templates = await _service.GetAllAsync();

        var result = templates.Select(t => new
        {
            t.EmailTemplateId,
            t.EmailTemplateGuid,
            t.TemplateName,
            t.BodyText,
            t.BodyHtml,
            Links = new
            {
                Self = Url.Action(nameof(GetByGuid), new { guid = t.EmailTemplateGuid }),
                Update = Url.Action(nameof(Update), new { guid = t.EmailTemplateGuid }),
                Delete = Url.Action(nameof(Delete), new { guid = t.EmailTemplateGuid })
            }
        });

        return Ok(result);
    }

    [HttpGet("{guid}")]
    public async Task<IActionResult> GetByGuid(Guid guid)
    {
        var template = await _service.GetByGuidAsync(guid);

        if (template == null)
            return NotFound(new { error = "Template not found" });

        return Ok(template);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEmailTemplateRequest request)
    {
        var template = new EmailTemplate
        {
            TemplateName = request.TemplateName,
            BodyText = request.BodyText,
            BodyHtml = request.BodyHtml
        };

        await _service.CreateAsync(template);

        return CreatedAtAction(nameof(GetByGuid), new { guid = template.EmailTemplateGuid }, template);
    }

    [HttpPut("{guid}")]
    public async Task<IActionResult> Update(Guid guid, [FromBody] UpdateEmailTemplateRequest request)
    {
        var template = await _service.UpdateAsync(guid, request.TemplateName, request.BodyText, request.BodyHtml);

        if (template == null)
            return NotFound(new { error = "Template not found" });

        return Ok(template);
    }

    [HttpDelete("{guid}")]
    public async Task<IActionResult> Delete(Guid guid)
    {
        var deleted = await _service.DeleteAsync(guid);

        if (!deleted)
            return NotFound(new { error = "Template not found" });

        return Ok(new { message = "Template deleted" });
    }
}

public class CreateEmailTemplateRequest
{
    public required string TemplateName { get; set; }
    public string BodyText { get; set; } = "";
    public string BodyHtml { get; set; } = "";
}

public class UpdateEmailTemplateRequest
{
    public required string TemplateName { get; set; }
    public string BodyText { get; set; } = "";
    public string BodyHtml { get; set; } = "";
}
