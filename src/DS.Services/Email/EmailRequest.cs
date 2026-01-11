namespace DS.Services.Email;

public class EmailRequest
{
    // Recipient Information
    public List<string> ToEmails { get; set; } = new();
    public List<string> CcEmails { get; set; } = new();
    public List<string> BccEmails { get; set; } = new();

    // Email Content
    public string Subject { get; set; }
    public string TextBody { get; set; }
    public string HtmlBody { get; set; }

    // Attachments
    public List<EmailAttachment> Attachments { get; set; }
}