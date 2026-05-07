using AutoMailerBackend.Clients;
using AutoMailerBackend.Models;

namespace AutoMailerBackend.Services;

public class EmailService
{
    private readonly Smtp2GoClient _smtp2GoClient;

    public EmailService(Smtp2GoClient smtp2GoClient)
    {
        _smtp2GoClient = smtp2GoClient;
    }

    public async Task<Smtp2GoResponse> SendEmailAsync(SendEmailRequest request)
    {
        var isHtml = request.Body.TrimStart().StartsWith("<", StringComparison.Ordinal);

        if (isHtml)
            return await _smtp2GoClient.SendEmailAsync(request.To, request.ToName, request.Subject, "", request.Body);

        return await _smtp2GoClient.SendEmailAsync(request.To, request.ToName, request.Subject, request.Body);
    }
}
