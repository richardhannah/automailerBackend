namespace AutoMailerBackend.Clients;

public class Smtp2GoSettings
{
    public required string ApiKey { get; set; }
    public required string SenderName { get; set; }
    public required string SenderEmail { get; set; }
}

public class Smtp2GoClient
{
    private readonly HttpClient _httpClient;
    private readonly Smtp2GoSettings _settings;

    public Smtp2GoClient(HttpClient httpClient, Smtp2GoSettings settings)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://api.smtp2go.com");
        _httpClient.DefaultRequestHeaders.Add("X-Smtp2go-Api-Key", settings.ApiKey);
        _httpClient.DefaultRequestHeaders.Add("accept", "application/json");
        _settings = settings;
    }

    public async Task<Smtp2GoResponse> SendEmailAsync(string toEmail, string toName, string subject, string textBody, string? htmlBody = null)
    {
        var payload = new Dictionary<string, object>
        {
            ["sender"] = $"{_settings.SenderName} <{_settings.SenderEmail}>",
            ["to"] = new[] { $"{toName} <{toEmail}>" },
            ["subject"] = subject,
            ["text_body"] = textBody
        };
        if (!string.IsNullOrEmpty(htmlBody))
            payload["html_body"] = htmlBody;

        var response = await _httpClient.PostAsJsonAsync("/v3/email/send", payload);
        var body = await response.Content.ReadAsStringAsync();

        return new Smtp2GoResponse
        {
            Success = response.IsSuccessStatusCode,
            StatusCode = (int)response.StatusCode,
            Body = body
        };
    }
}

public class Smtp2GoResponse
{
    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public string Body { get; set; } = string.Empty;
}
