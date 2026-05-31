namespace AutoMailerBackend.Models;

public class SocialMediaLink
{
    public int SocialMediaLinkId { get; set; }
    public string Platform { get; set; } = "";
    public string Url { get; set; } = "";
}
