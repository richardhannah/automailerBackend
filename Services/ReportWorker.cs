using AutoMailerBackend.Clients;
using AutoMailerBackend.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoMailerBackend.Services;

public class ReportWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReportWorker> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(24);

    public ReportWorker(IServiceScopeFactory scopeFactory, ILogger<ReportWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ReportWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunReportAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running expiration report");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task RunReportAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var smtp2GoClient = scope.ServiceProvider.GetRequiredService<Smtp2GoClient>();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var cutoff = today.AddDays(7);

        var expiringCustomers = await db.Customers
            .Where(c => c.ExpirationDate != null && c.ExpirationDate >= today && c.ExpirationDate <= cutoff)
            .OrderBy(c => c.ExpirationDate)
            .ToListAsync(ct);

        if (expiringCustomers.Count == 0)
        {
            _logger.LogInformation("No customers expiring within 7 days");
            return;
        }

        _logger.LogInformation("Found {Count} customers expiring within 7 days", expiringCustomers.Count);

        var reportSettings = await db.ReportingSettings
            .Include(r => r.EmailTemplate)
            .Where(r => r.EmailTemplateId != null)
            .ToListAsync(ct);

        if (reportSettings.Count == 0)
        {
            _logger.LogWarning("No reporting settings with templates configured");
            return;
        }

        // Build the customers collection for {% for %} loops
        var customersCollection = expiringCustomers.Select(c => new Dictionary<string, string>
        {
            ["firstName"] = c.FirstName,
            ["lastName"] = c.LastName,
            ["name"] = $"{c.FirstName} {c.LastName}",
            ["email"] = c.Email,
            ["iptvUser"] = c.IptvUser,
            ["iptvPassword"] = c.IptvPassword ?? "",
            ["notes"] = c.Notes ?? "",
            ["expirationDate"] = c.ExpirationDate?.ToString("yyyy-MM-dd") ?? "",
        }).ToList();

        var collections = new Dictionary<string, List<Dictionary<string, string>>>
        {
            ["customers"] = customersCollection
        };

        var vars = new Dictionary<string, string>();

        foreach (var setting in reportSettings)
        {
            var template = setting.EmailTemplate!;

            var subject = "Expiration Report";
            string? textBody = null;
            string? htmlBody = null;

            if (!string.IsNullOrEmpty(template.BodyText))
                textBody = TemplateRenderer.Render(template.BodyText, vars, collections);

            if (!string.IsNullOrEmpty(template.BodyHtml))
                htmlBody = TemplateRenderer.Render(template.BodyHtml, vars, collections);

            var result = await smtp2GoClient.SendEmailAsync(
                setting.EmailAddress,
                setting.Name,
                subject,
                textBody ?? htmlBody ?? "",
                htmlBody
            );

            if (result.Success)
                _logger.LogInformation("Sent expiration report to {Email}", setting.EmailAddress);
            else
                _logger.LogWarning("Failed to send expiration report to {Email}: {Status} {Body}",
                    setting.EmailAddress, result.StatusCode, result.Body);
        }
    }
}
