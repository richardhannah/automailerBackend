using AutoMailerBackend.Clients;
using AutoMailerBackend.Controllers;
using AutoMailerBackend.Data;
using AutoMailerBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoMailerBackend.Services;

public class EnquiriesService
{
    private readonly AppDbContext _db;
    private readonly Smtp2GoClient _smtpClient;
    private readonly ILogger<EnquiriesService> _logger;

    public EnquiriesService(AppDbContext db, Smtp2GoClient smtpClient, ILogger<EnquiriesService> logger)
    {
        _db = db;
        _smtpClient = smtpClient;
        _logger = logger;
    }

    public async Task<List<Enquiry>> GetAllAsync()
    {
        return await _db.Enquiries.OrderByDescending(e => e.DateReceived).ToListAsync();
    }

    public async Task<Enquiry?> GetByIdAsync(int id)
    {
        return await _db.Enquiries.FindAsync(id);
    }

    public async Task<Enquiry> CreateAsync(CreateEnquiryRequest request)
    {
        var enquiry = new Enquiry
        {
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            DateReceived = DateTime.UtcNow,
            Message = request.Message
        };

        _db.Enquiries.Add(enquiry);
        await _db.SaveChangesAsync();

        await SendCustomerConfirmationAsync(enquiry);
        await SendAdminNotificationsAsync(enquiry);

        return enquiry;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var enquiry = await _db.Enquiries.FindAsync(id);
        if (enquiry == null)
            return false;

        _db.Enquiries.Remove(enquiry);
        await _db.SaveChangesAsync();
        return true;
    }

    private async Task SendCustomerConfirmationAsync(Enquiry enquiry)
    {
        var setting = await _db.WorkflowEmailSettings
            .Include(w => w.EmailTemplate)
            .FirstOrDefaultAsync(w => w.WorkflowType == "Enquiry" && w.RecipientType == "Customer");

        if (setting?.EmailTemplate == null)
            return;

        var vars = new Dictionary<string, string>
        {
            ["enquiry.email"] = enquiry.Email,
            ["enquiry.phone"] = enquiry.PhoneNumber ?? "Not provided",
            ["enquiry.message"] = enquiry.Message,
            ["enquiry.date"] = enquiry.DateReceived.ToString("yyyy-MM-dd HH:mm")
        };

        var subject = "Thank you for your enquiry";
        var body = TemplateRenderer.Render(setting.EmailTemplate.BodyHtml, vars);

        try
        {
            await _smtpClient.SendEmailAsync(enquiry.Email, enquiry.Email, subject, "", body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send customer confirmation to {Email}", enquiry.Email);
        }
    }

    private async Task SendAdminNotificationsAsync(Enquiry enquiry)
    {
        var setting = await _db.WorkflowEmailSettings
            .Include(w => w.EmailTemplate)
            .FirstOrDefaultAsync(w => w.WorkflowType == "Enquiry" && w.RecipientType == "Admin");

        var recipientUserIds = await _db.WorkflowNotificationRecipients
            .Where(r => r.WorkflowType == "Enquiry")
            .Select(r => r.UserId)
            .ToListAsync();

        if (recipientUserIds.Count == 0)
            return;

        var recipients = await _db.Users
            .Where(u => recipientUserIds.Contains(u.UserId) && u.Email != "")
            .ToListAsync();

        if (recipients.Count == 0)
            return;

        var vars = new Dictionary<string, string>
        {
            ["enquiry.email"] = enquiry.Email,
            ["enquiry.phone"] = enquiry.PhoneNumber ?? "Not provided",
            ["enquiry.message"] = enquiry.Message,
            ["enquiry.date"] = enquiry.DateReceived.ToString("yyyy-MM-dd HH:mm")
        };

        string subject;
        string body;

        if (setting?.EmailTemplate != null)
        {
            subject = $"New Enquiry from {enquiry.Email}";
            body = TemplateRenderer.Render(setting.EmailTemplate.BodyHtml, vars);
        }
        else
        {
            subject = $"New Enquiry from {enquiry.Email}";
            body = $"""
                <h2>New Enquiry Received</h2>
                <p><strong>From:</strong> {enquiry.Email}</p>
                <p><strong>Phone:</strong> {enquiry.PhoneNumber ?? "Not provided"}</p>
                <p><strong>Date:</strong> {enquiry.DateReceived:yyyy-MM-dd HH:mm} UTC</p>
                <hr>
                <p>{enquiry.Message}</p>
                """;
        }

        foreach (var recipient in recipients)
        {
            try
            {
                await _smtpClient.SendEmailAsync(recipient.Email, recipient.Email, subject, "", body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send enquiry notification to {Email}", recipient.Email);
            }
        }
    }
}
