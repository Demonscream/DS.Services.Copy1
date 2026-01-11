namespace DS.Services.Email;

public class DSEmailServiceOptions
{
    public string ServiceKey { get; set; }
    public string SmtpServer { get; set; } = String.Empty;
    public string UserName { get; set; } = String.Empty;
    public string Password { get; set; } = String.Empty;
    public string From { get; set; } = String.Empty;
    public string FromName { get; set; } = String.Empty;
    public List<string> CcEmailAddresses { get; set; } = new ();
    public List<string> BccEmailAddresses { get; set; } = new ();
}