namespace AutoMailerBackend.Models;

public class ChatMessage
{
    public int ChatMessageId { get; set; }
    public string SenderName { get; set; } = "";
    public bool IsAdmin { get; set; }
    public string Text { get; set; } = "";
    public DateTime SentAt { get; set; }
}
