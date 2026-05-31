using AutoMailerBackend.Data;
using AutoMailerBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoMailerBackend.Services;

public class SocialMediaLinksService
{
    private readonly AppDbContext _db;

    public SocialMediaLinksService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<SocialMediaLink>> GetAllAsync()
    {
        return await _db.SocialMediaLinks
            .OrderBy(s => s.Platform)
            .ToListAsync();
    }

    public async Task SaveAsync(List<SocialMediaLinkDto> links)
    {
        var existing = await _db.SocialMediaLinks.ToListAsync();

        foreach (var dto in links)
        {
            var platform = dto.Platform.Trim().ToLowerInvariant();
            var url = dto.Url?.Trim() ?? "";

            var record = existing.FirstOrDefault(e =>
                e.Platform.Equals(platform, StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrEmpty(url))
            {
                if (record != null)
                    _db.SocialMediaLinks.Remove(record);
            }
            else if (record != null)
            {
                record.Url = url;
            }
            else
            {
                _db.SocialMediaLinks.Add(new SocialMediaLink
                {
                    Platform = platform,
                    Url = url
                });
            }
        }

        await _db.SaveChangesAsync();
    }
}

public class SocialMediaLinkDto
{
    public string Platform { get; set; } = "";
    public string? Url { get; set; }
}
