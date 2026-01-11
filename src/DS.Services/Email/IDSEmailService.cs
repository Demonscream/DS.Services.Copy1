namespace DS.Services.Email;

public interface IDSEmailService : IDisposable
{
    Task SendAsync(EmailRequest request);
}