using AutoMailerBackend.Data;
using AutoMailerBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoMailerBackend.Services;

public class EmailTemplatesService
{
    private readonly AppDbContext _db;

    public EmailTemplatesService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<EmailTemplate>> GetAllAsync()
    {
        return await _db.EmailTemplates.ToListAsync();
    }

    public async Task<EmailTemplate?> GetByGuidAsync(Guid guid)
    {
        return await _db.EmailTemplates
            .FirstOrDefaultAsync(t => t.EmailTemplateGuid == guid);
    }

    public async Task<EmailTemplate> CreateAsync(EmailTemplate template)
    {
        _db.EmailTemplates.Add(template);
        await _db.SaveChangesAsync();
        return template;
    }

    public async Task<EmailTemplate?> UpdateAsync(Guid guid, string templateName, string bodyText, string bodyHtml)
    {
        var template = await _db.EmailTemplates
            .FirstOrDefaultAsync(t => t.EmailTemplateGuid == guid);

        if (template == null)
            return null;

        template.TemplateName = templateName;
        template.BodyText = bodyText;
        template.BodyHtml = bodyHtml;

        await _db.SaveChangesAsync();
        return template;
    }

    public async Task<bool> DeleteAsync(Guid guid)
    {
        var template = await _db.EmailTemplates
            .FirstOrDefaultAsync(t => t.EmailTemplateGuid == guid);

        if (template == null)
            return false;

        _db.EmailTemplates.Remove(template);
        await _db.SaveChangesAsync();
        return true;
    }
}
