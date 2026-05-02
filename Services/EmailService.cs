using AutoMailerBackend.Clients;
using AutoMailerBackend.Models;

namespace AutoMailerBackend.Services;

public class EmailService
{
    private readonly BrevoClient _brevoClient;

    public EmailService(BrevoClient brevoClient)
    {
        _brevoClient = brevoClient;
    }

    public async Task<BrevoResponse> SendEmailAsync(SendEmailRequest request)
    {
        return await _brevoClient.SendEmailAsync(request.To, request.ToName, request.Subject, request.Body);
    }
}
