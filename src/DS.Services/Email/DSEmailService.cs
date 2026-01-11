namespace DS.Services.Email;

public class DSEmailService(ILogger<DSEmailService> logger, IOptions<DSEmailServiceOptions> options, ISmtpClient smtpClient) : IDSEmailService, IDisposable
{
    private readonly DSEmailServiceOptions _options = options.Value;
    private bool disposedValue;

    public async Task SendAsync(EmailRequest request)
    {
        ArgumentNullException.ThrowIfNull(nameof(request));
        
        request.ToEmails = request.ToEmails.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        _ = request.ToEmails.Count < 1 ? throw new InvalidOperationException("No email 'To' addresses have been provided.") : 0;
        _ = string.IsNullOrWhiteSpace(request.Subject) ? throw new InvalidOperationException("The email request must have a subject.") : 0;
        
        var msg = new MimeMessage();

        msg.To.AddRange(request.ToEmails.Select(x => new MailboxAddress(string.Empty, x)));
        msg.Cc.AddRange(MergeAndDeDupe(_options.CcEmailAddresses, request.CcEmails).Select(x => new MailboxAddress(string.Empty, x)));
        msg.Bcc.AddRange(MergeAndDeDupe(_options.BccEmailAddresses, request.BccEmails).Select(x => new MailboxAddress(string.Empty, x)));
        msg.From.Add(new MailboxAddress(_options.FromName, _options.From));
        msg.Subject = request.Subject;
        
        var builder = new BodyBuilder
        {
            HtmlBody = request.HtmlBody,
            TextBody = request.TextBody
        };

        // Add attachments
        if (request.Attachments != null && request.Attachments.Any())
        {
            foreach (var attachment in request.Attachments)
            {
                AddAttachment(builder, attachment);
            }
        }

        msg.Body = builder.ToMessageBody();

        await SendAsync(msg);
    }

    #region IDisposable Implementation
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // Dispose managed state (managed objects).
                smtpClient.Dispose();
            }
            
            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    #endregion

    private void AddAttachment(BodyBuilder builder, EmailAttachment attachment)
    {
        var contentType = string.IsNullOrEmpty(attachment.ContentType)
            ? GetContentType(attachment.FileName)
            : ContentType.Parse(attachment.ContentType);

        // Handle file path
        if (!string.IsNullOrEmpty(attachment.FilePath))
        {
            if (!File.Exists(attachment.FilePath))
            {
                throw new FileNotFoundException($"File not found: {attachment.FilePath}");
            }

            builder.Attachments.Add(attachment.FilePath);
        }
        // Handle byte array
        else if (attachment.FileBytes is { Length: > 0 })
        {
            if (string.IsNullOrEmpty(attachment.FileName))
            {
                throw new ArgumentException("FileName is required when using FileBytes");
            }

            builder.Attachments.Add(attachment.FileName, attachment.FileBytes, contentType);
        }
        // Handle stream
        else if (attachment.FileStream != null)
        {
            if (string.IsNullOrEmpty(attachment.FileName))
            {
                throw new ArgumentException("FileName is required when using FileStream");
            }

            // Reset stream position if possible
            if (attachment.FileStream.CanSeek)
            {
                attachment.FileStream.Position = 0;
            }

            builder.Attachments.Add(attachment.FileName, attachment.FileStream, contentType);
        }
        else
        {
            throw new ArgumentException("Attachment must have either FilePath, FileBytes, or FileStream");
        }
    }
    /// <summary>
    /// Send as an asynchronous operation.
    /// </summary>
    /// <param name="mailMessage">The mail message.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    protected virtual async Task SendAsync(MimeMessage mailMessage)
    {
        try
        {
            await smtpClient.ConnectAsync(_options.SmtpServer, 0, SecureSocketOptions.None);
            await smtpClient.AuthenticateAsync(_options.UserName, _options.Password);

            await smtpClient.SendAsync(mailMessage);
            await smtpClient.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
            throw;
        }
    }

    private List<string> MergeAndDeDupe(List<string> list1, List<string> list2)
    {
        ArgumentNullException.ThrowIfNull(list1);
        ArgumentNullException.ThrowIfNull(list2);

        var newList = new List<string>();
        newList.AddRangeWithoutEmptyAndDuplicates(list1);
        newList.AddRangeWithoutEmptyAndDuplicates(list2);

        return newList;
    }

    private ContentType GetContentType(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return ContentType.Parse("application/octet-stream");
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        return extension switch
        {
            ".pdf" => ContentType.Parse("application/pdf"),
            ".txt" => ContentType.Parse("text/plain"),
            ".doc" => ContentType.Parse("application/msword"),
            ".docx" => ContentType.Parse("application/vnd.openxmlformats-officedocument.wordprocessingml.document"),
            ".xls" => ContentType.Parse("application/vnd.ms-excel"),
            ".xlsx" => ContentType.Parse("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"),
            ".csv" => ContentType.Parse("text/csv"),
            ".jpg" or ".jpeg" => ContentType.Parse("image/jpeg"),
            ".png" => ContentType.Parse("image/png"),
            ".gif" => ContentType.Parse("image/gif"),
            ".zip" => ContentType.Parse("application/zip"),
            ".json" => ContentType.Parse("application/json"),
            ".xml" => ContentType.Parse("application/xml"),
            _ => ContentType.Parse("application/octet-stream")
        };
    }

}