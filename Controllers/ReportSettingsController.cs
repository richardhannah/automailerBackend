using AutoMailerBackend.Auth;
using AutoMailerBackend.Models;
using AutoMailerBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoMailerBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[TokenAuth]
[RequireRole(UserRole.Admin)]
public class ReportSettingsController : ControllerBase
{
    private readonly ReportSettingsService _service;

    public ReportSettingsController(ReportSettingsService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var settings = await _service.GetAllAsync();

        var result = settings.Select(r => new
        {
            r.ReportingSettingId,
            r.Name,
            r.EmailAddress,
            r.EmailTemplateId,
            EmailTemplateName = r.EmailTemplate?.TemplateName,
            Links = new
            {
                Self = Url.Action(nameof(GetById), new { id = r.ReportingSettingId }),
                Update = Url.Action(nameof(Update), new { id = r.ReportingSettingId }),
                Delete = Url.Action(nameof(Delete), new { id = r.ReportingSettingId })
            }
        });

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var setting = await _service.GetByIdAsync(id);

        if (setting == null)
            return NotFound(new { error = "Report setting not found" });

        return Ok(new
        {
            setting.ReportingSettingId,
            setting.Name,
            setting.EmailAddress,
            setting.EmailTemplateId,
            EmailTemplateName = setting.EmailTemplate?.TemplateName
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReportSettingRequest request)
    {
        var result = await _service.CreateAsync(request.Name, request.EmailAddress, request.EmailTemplateId);

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        var setting = result.Setting!;
        return CreatedAtAction(nameof(GetById), new { id = setting.ReportingSettingId }, new
        {
            setting.ReportingSettingId,
            setting.Name,
            setting.EmailAddress,
            setting.EmailTemplateId
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateReportSettingRequest request)
    {
        var result = await _service.UpdateAsync(id, request.Name, request.EmailAddress, request.EmailTemplateId);

        if (!result.Found)
            return NotFound(new { error = "Report setting not found" });

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        var setting = result.Setting!;
        return Ok(new
        {
            setting.ReportingSettingId,
            setting.Name,
            setting.EmailAddress,
            setting.EmailTemplateId
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id);

        if (!deleted)
            return NotFound(new { error = "Report setting not found" });

        return Ok(new { message = "Report setting deleted" });
    }
}

public class CreateReportSettingRequest
{
    public required string Name { get; set; }
    public required string EmailAddress { get; set; }
    public int? EmailTemplateId { get; set; }
}

public class UpdateReportSettingRequest
{
    public required string Name { get; set; }
    public required string EmailAddress { get; set; }
    public int? EmailTemplateId { get; set; }
}
