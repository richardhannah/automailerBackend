using AutoMailerBackend.Data;
using AutoMailerBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoMailerBackend.Services;

public class ReportSettingsService
{
    private readonly AppDbContext _db;

    public ReportSettingsService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<ReportingSetting>> GetAllAsync()
    {
        return await _db.ReportingSettings
            .Include(r => r.EmailTemplate)
            .ToListAsync();
    }

    public async Task<ReportingSetting?> GetByIdAsync(int id)
    {
        return await _db.ReportingSettings
            .Include(r => r.EmailTemplate)
            .FirstOrDefaultAsync(r => r.ReportingSettingId == id);
    }

    public async Task<ReportSettingCreateResult> CreateAsync(string name, string emailAddress, int? emailTemplateId)
    {
        if (emailTemplateId.HasValue)
        {
            var templateExists = await _db.EmailTemplates
                .AnyAsync(t => t.EmailTemplateId == emailTemplateId.Value);
            if (!templateExists)
                return new ReportSettingCreateResult { Success = false, Error = "Email template not found" };
        }

        var setting = new ReportingSetting
        {
            Name = name,
            EmailAddress = emailAddress,
            EmailTemplateId = emailTemplateId
        };

        _db.ReportingSettings.Add(setting);
        await _db.SaveChangesAsync();

        return new ReportSettingCreateResult { Success = true, Setting = setting };
    }

    public async Task<ReportSettingUpdateResult> UpdateAsync(int id, string name, string emailAddress, int? emailTemplateId)
    {
        var setting = await _db.ReportingSettings
            .FirstOrDefaultAsync(r => r.ReportingSettingId == id);

        if (setting == null)
            return new ReportSettingUpdateResult { Found = false };

        if (emailTemplateId.HasValue)
        {
            var templateExists = await _db.EmailTemplates
                .AnyAsync(t => t.EmailTemplateId == emailTemplateId.Value);
            if (!templateExists)
                return new ReportSettingUpdateResult { Found = true, Success = false, Error = "Email template not found" };
        }

        setting.Name = name;
        setting.EmailAddress = emailAddress;
        setting.EmailTemplateId = emailTemplateId;

        await _db.SaveChangesAsync();
        return new ReportSettingUpdateResult { Found = true, Success = true, Setting = setting };
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var setting = await _db.ReportingSettings
            .FirstOrDefaultAsync(r => r.ReportingSettingId == id);

        if (setting == null)
            return false;

        _db.ReportingSettings.Remove(setting);
        await _db.SaveChangesAsync();
        return true;
    }
}

public class ReportSettingCreateResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public ReportingSetting? Setting { get; set; }
}

public class ReportSettingUpdateResult
{
    public bool Found { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
    public ReportingSetting? Setting { get; set; }
}
