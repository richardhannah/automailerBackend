namespace AutoMailerBackend.Clients;

public class BrevoSettings
{
    public required string ApiKey { get; set; }
    public required string SenderName { get; set; }
    public required string SenderEmail { get; set; }
}

public class BrevoClient
{https://app.smtp2go.com/sending/verified_senders/#edit/ronmar.com.au
    private readonly HttpClient _httpClient;
    private readonly BrevoSettings _settings;

    public BrevoClient(HttpClient httpClient, BrevoSettings settings)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://api.brevo.com");
        _httpClient.DefaultRequestHeaders.Add("api-key", settings.ApiKey);
        _httpClient.DefaultRequestHeaders.Add("accept", "application/json");
        _settings = settings;
    }

    public async Task<BrevoResponse> SendEmailAsync(string toEmail, string toName, string subject, string textContent, string? htmlContent = null)
    {
        var payload = new Dictionary<string, object>
        {
            ["sender"] = new { name = _settings.SenderName, email = _settings.SenderEmail },
            ["to"] = new[] { new { name = toName, email = toEmail } },
            ["subject"] = subject,
            ["textContent"] = textContent
        };
        if (!string.IsNullOrEmpty(htmlContent))
            payload["htmlContent"] = htmlContent;

        var response = await _httpClient.PostAsJsonAsync("/v3/smtp/email", payload);
        var body = await response.Content.ReadAsStringAsync();

        return new BrevoResponse
        {
            Success = response.IsSuccessStatusCode,
            StatusCode = (int)response.StatusCode,
            Body = body
        };
    }
}

public class BrevoResponse
{
    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public string Body { get; set; } = string.Empty;
}
