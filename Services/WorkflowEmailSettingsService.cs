using AutoMailerBackend.Data;
using AutoMailerBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoMailerBackend.Services;

public class WorkflowEmailSettingsService
{
    private readonly AppDbContext _db;

    public WorkflowEmailSettingsService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<WorkflowEmailSetting>> GetByWorkflowAsync(string workflowType)
    {
        return await _db.WorkflowEmailSettings
            .Include(w => w.EmailTemplate)
            .Where(w => w.WorkflowType == workflowType)
            .ToListAsync();
    }

    public async Task<WorkflowEmailSetting?> GetAsync(string workflowType, string recipientType)
    {
        return await _db.WorkflowEmailSettings
            .Include(w => w.EmailTemplate)
            .FirstOrDefaultAsync(w => w.WorkflowType == workflowType && w.RecipientType == recipientType);
    }

    public async Task<WorkflowEmailSettingResult> UpsertAsync(string workflowType, string recipientType, int? emailTemplateId)
    {
        if (emailTemplateId.HasValue)
        {
            var templateExists = await _db.EmailTemplates
                .AnyAsync(t => t.EmailTemplateId == emailTemplateId.Value);
            if (!templateExists)
                return new WorkflowEmailSettingResult { Success = false, Error = "Email template not found" };
        }

        var setting = await _db.WorkflowEmailSettings
            .FirstOrDefaultAsync(w => w.WorkflowType == workflowType && w.RecipientType == recipientType);

        if (setting == null)
        {
            setting = new WorkflowEmailSetting
            {
                WorkflowType = workflowType,
                RecipientType = recipientType,
                EmailTemplateId = emailTemplateId
            };
            _db.WorkflowEmailSettings.Add(setting);
        }
        else
        {
            setting.EmailTemplateId = emailTemplateId;
        }

        await _db.SaveChangesAsync();

        setting = await _db.WorkflowEmailSettings
            .Include(w => w.EmailTemplate)
            .FirstAsync(w => w.WorkflowEmailSettingId == setting.WorkflowEmailSettingId);

        return new WorkflowEmailSettingResult { Success = true, Setting = setting };
    }

    public async Task<List<Guid>> GetRecipientsAsync(string workflowType)
    {
        return await _db.WorkflowNotificationRecipients
            .Where(r => r.WorkflowType == workflowType)
            .Select(r => r.UserId)
            .ToListAsync();
    }

    public async Task SetRecipientsAsync(string workflowType, List<Guid> userIds)
    {
        var existing = await _db.WorkflowNotificationRecipients
            .Where(r => r.WorkflowType == workflowType)
            .ToListAsync();

        _db.WorkflowNotificationRecipients.RemoveRange(existing);

        foreach (var userId in userIds)
        {
            _db.WorkflowNotificationRecipients.Add(new WorkflowNotificationRecipient
            {
                WorkflowType = workflowType,
                UserId = userId
            });
        }

        await _db.SaveChangesAsync();
    }

    public async Task AddRecipientAsync(string workflowType, Guid userId)
    {
        var exists = await _db.WorkflowNotificationRecipients
            .AnyAsync(r => r.WorkflowType == workflowType && r.UserId == userId);
        if (exists) return;

        _db.WorkflowNotificationRecipients.Add(new WorkflowNotificationRecipient
        {
            WorkflowType = workflowType,
            UserId = userId
        });
        await _db.SaveChangesAsync();
    }

    public async Task RemoveRecipientAsync(string workflowType, Guid userId)
    {
        var recipient = await _db.WorkflowNotificationRecipients
            .FirstOrDefaultAsync(r => r.WorkflowType == workflowType && r.UserId == userId);
        if (recipient == null) return;

        _db.WorkflowNotificationRecipients.Remove(recipient);
        await _db.SaveChangesAsync();
    }
}

public class WorkflowEmailSettingResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public WorkflowEmailSetting? Setting { get; set; }
}
