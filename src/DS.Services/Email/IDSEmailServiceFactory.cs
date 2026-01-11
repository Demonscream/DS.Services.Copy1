namespace DS.Services.Email;

public interface IDSEmailServiceFactory
{
    IDSEmailService Create(string serviceKey);
}