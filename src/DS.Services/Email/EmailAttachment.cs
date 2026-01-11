namespace DS.Services.Email;

public class EmailAttachment
{
    public string FileName { get; set; }
    public string FilePath { get; set; }
    public byte[] FileBytes { get; set; }
    public Stream FileStream { get; set; }
    public string ContentType { get; set; }
}