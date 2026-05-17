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

    private static readonly string[] NotificationRecipients = ["ron@ronmar.com.au", "fiona.hannah@pm.me"];

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

        await SendNotificationEmailsAsync(enquiry);

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

    private async Task SendNotificationEmailsAsync(Enquiry enquiry)
    {
        var subject = $"New Enquiry from {enquiry.Email}";
        var body = $"""
            <h2>New Enquiry Received</h2>
            <p><strong>From:</strong> {enquiry.Email}</p>
            <p><strong>Phone:</strong> {enquiry.PhoneNumber ?? "Not provided"}</p>
            <p><strong>Date:</strong> {enquiry.DateReceived:yyyy-MM-dd HH:mm} UTC</p>
            <hr>
            <p>{enquiry.Message}</p>
            """;

        foreach (var recipient in NotificationRecipients)
        {
            try
            {
                await _smtpClient.SendEmailAsync(recipient, recipient, subject, "", body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send enquiry notification to {Recipient}", recipient);
            }
        }
    }
}
