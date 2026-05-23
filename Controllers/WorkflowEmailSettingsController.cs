using AutoMailerBackend.Auth;
using AutoMailerBackend.Models;
using AutoMailerBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoMailerBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[TokenAuth]
[RequireRole(UserRole.Admin)]
public class WorkflowEmailSettingsController : ControllerBase
{
    private readonly WorkflowEmailSettingsService _service;

    public WorkflowEmailSettingsController(WorkflowEmailSettingsService service)
    {
        _service = service;
    }

    [HttpGet("{workflowType}")]
    public async Task<IActionResult> GetByWorkflow(string workflowType)
    {
        var settings = await _service.GetByWorkflowAsync(workflowType);

        var result = settings.Select(s => new
        {
            s.WorkflowEmailSettingId,
            s.WorkflowType,
            s.RecipientType,
            s.EmailTemplateId,
            EmailTemplateName = s.EmailTemplate?.TemplateName
        });

        return Ok(result);
    }

    [HttpPut("{workflowType}/{recipientType}")]
    public async Task<IActionResult> Upsert(string workflowType, string recipientType, [FromBody] UpsertWorkflowEmailSettingRequest request)
    {
        var result = await _service.UpsertAsync(workflowType, recipientType, request.EmailTemplateId);

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        var setting = result.Setting!;
        return Ok(new
        {
            setting.WorkflowEmailSettingId,
            setting.WorkflowType,
            setting.RecipientType,
            setting.EmailTemplateId,
            EmailTemplateName = setting.EmailTemplate?.TemplateName
        });
    }
    [HttpGet("{workflowType}/recipients")]
    public async Task<IActionResult> GetRecipients(string workflowType)
    {
        var userIds = await _service.GetRecipientsAsync(workflowType);
        return Ok(userIds);
    }

    [HttpPut("{workflowType}/recipients/{userId}")]
    public async Task<IActionResult> AddRecipient(string workflowType, Guid userId)
    {
        await _service.AddRecipientAsync(workflowType, userId);
        return Ok(new { message = "Recipient added" });
    }

    [HttpDelete("{workflowType}/recipients/{userId}")]
    public async Task<IActionResult> RemoveRecipient(string workflowType, Guid userId)
    {
        await _service.RemoveRecipientAsync(workflowType, userId);
        return Ok(new { message = "Recipient removed" });
    }
}

public class UpsertWorkflowEmailSettingRequest
{
    public int? EmailTemplateId { get; set; }
}
