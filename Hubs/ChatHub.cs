using Microsoft.AspNetCore.SignalR;
using AutoMailerBackend.Data;
using AutoMailerBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoMailerBackend.Hubs;

public class ChatHub : Hub
{
    private readonly IServiceScopeFactory _scopeFactory;

    public ChatHub(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
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
        Console.WriteLine($"Chat client disconnected ({Context.ConnectionId})");
        await base.OnDisconnectedAsync(exception);
    }
}
