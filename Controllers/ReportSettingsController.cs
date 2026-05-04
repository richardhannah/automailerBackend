using AutoMailerBackend.Auth;
using AutoMailerBackend.Data;
using AutoMailerBackend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoMailerBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[TokenAuth]
[RequireRole(UserRole.Admin)]
public class ReportSettingsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ReportSettingsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var settings = await _db.ReportingSettings
            .Include(r => r.EmailTemplate)
            .ToListAsync();

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
        var setting = await _db.ReportingSettings
            .Include(r => r.EmailTemplate)
            .FirstOrDefaultAsync(r => r.ReportingSettingId == id);

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
        if (request.EmailTemplateId.HasValue)
        {
            var templateExists = await _db.EmailTemplates
                .AnyAsync(t => t.EmailTemplateId == request.EmailTemplateId.Value);
            if (!templateExists)
                return BadRequest(new { error = "Email template not found" });
        }

        var setting = new ReportingSetting
        {
            Name = request.Name,
            EmailAddress = request.EmailAddress,
            EmailTemplateId = request.EmailTemplateId
        };

        _db.ReportingSettings.Add(setting);
        await _db.SaveChangesAsync();

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
        var setting = await _db.ReportingSettings
            .FirstOrDefaultAsync(r => r.ReportingSettingId == id);

        if (setting == null)
            return NotFound(new { error = "Report setting not found" });

        if (request.EmailTemplateId.HasValue)
        {
            var templateExists = await _db.EmailTemplates
                .AnyAsync(t => t.EmailTemplateId == request.EmailTemplateId.Value);
            if (!templateExists)
                return BadRequest(new { error = "Email template not found" });
        }

        setting.Name = request.Name;
        setting.EmailAddress = request.EmailAddress;
        setting.EmailTemplateId = request.EmailTemplateId;

        await _db.SaveChangesAsync();
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
        var setting = await _db.ReportingSettings
            .FirstOrDefaultAsync(r => r.ReportingSettingId == id);

        if (setting == null)
            return NotFound(new { error = "Report setting not found" });

        _db.ReportingSettings.Remove(setting);
        await _db.SaveChangesAsync();

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
