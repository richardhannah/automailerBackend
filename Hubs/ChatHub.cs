using Microsoft.AspNetCore.SignalR;
using AutoMailerBackend.Clients;
using AutoMailerBackend.Data;
using AutoMailerBackend.Models;
using AutoMailerBackend.Services;
using Microsoft.EntityFrameworkCore;

namespace AutoMailerBackend.Hubs;

public class ChatHub : Hub
{
    private readonly IServiceScopeFactory _scopeFactory;

    private static readonly HashSet<string> _adminConnectionIds = new();
    private static readonly object _lock = new();

    public ChatHub(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public void JoinAsAdmin()
    {
        lock (_lock)
        {
            _adminConnectionIds.Add(Context.ConnectionId);
        }
        Console.WriteLine($"Admin joined chat ({Context.ConnectionId}), total admins: {_adminConnectionIds.Count}");
    }

    public async Task SendMessage(string senderName, bool isAdmin, string text)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var msg = new ChatMessage
        {
            SenderName = senderName,
            IsAdmin = isAdmin,
            Text = text,
            SentAt = DateTime.UtcNow
        };

        db.ChatMessages.Add(msg);
        await db.SaveChangesAsync();

        await Clients.All.SendAsync("ReceiveMessage", msg);

        if (!isAdmin)
        {
            bool hasAdmin;
            lock (_lock)
            {
                hasAdmin = _adminConnectionIds.Count > 0;
            }

            if (!hasAdmin)
            {
                _ = Task.Run(() => SendChatNotificationEmailAsync(senderName, text));
            }
        }
    }

    public async Task GetHistory()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var messages = await db.ChatMessages
            .OrderByDescending(m => m.SentAt)
            .Take(50)
            .OrderBy(m => m.SentAt)
            .ToListAsync();

        await Clients.Caller.SendAsync("ChatHistory", messages);
    }

    public override async Task OnConnectedAsync()
    {
        Console.WriteLine($"Chat client connected ({Context.ConnectionId})");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        lock (_lock)
        {
            _adminConnectionIds.Remove(Context.ConnectionId);
        }
        Console.WriteLine($"Chat client disconnected ({Context.ConnectionId}), total admins: {_adminConnectionIds.Count}");
        await base.OnDisconnectedAsync(exception);
    }

    private async Task SendChatNotificationEmailAsync(string senderName, string messageText)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var smtpClient = scope.ServiceProvider.GetRequiredService<Smtp2GoClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ChatHub>>();

            var setting = await db.WorkflowEmailSettings
                .Include(w => w.EmailTemplate)
                .FirstOrDefaultAsync(w => w.WorkflowType == "LiveChat" && w.RecipientType == "Admin");

            var recipientUserIds = await db.WorkflowNotificationRecipients
                .Where(r => r.WorkflowType == "LiveChat")
                .Select(r => r.UserId)
                .ToListAsync();

            if (recipientUserIds.Count == 0)
                return;

            var recipients = await db.Users
                .Where(u => recipientUserIds.Contains(u.UserId) && u.Email != "")
                .ToListAsync();

            if (recipients.Count == 0)
                return;

            var vars = new Dictionary<string, string>
            {
                ["chat.senderName"] = senderName,
                ["chat.message"] = messageText,
                ["chat.date"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm")
            };

            string subject = $"New Live Chat message from {senderName}";
            string body;

            if (setting?.EmailTemplate != null)
            {
                body = TemplateRenderer.Render(setting.EmailTemplate.BodyHtml, vars);
            }
            else
            {
                body = $"""
                    <h2>New Live Chat Message</h2>
                    <p><strong>From:</strong> {senderName}</p>
                    <p><strong>Date:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC</p>
                    <hr>
                    <p>{messageText}</p>
                    """;
            }

            foreach (var recipient in recipients)
            {
                try
                {
                    await smtpClient.SendEmailAsync(recipient.Email, recipient.Email, subject, "", body);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to send chat notification to {Email}", recipient.Email);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send chat notification email: {ex.Message}");
        }
    }
}
