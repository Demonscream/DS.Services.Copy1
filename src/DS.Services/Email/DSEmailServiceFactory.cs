namespace DS.Services.Email;

public class DSEmailServiceFactory(IServiceProvider serviceProvider) : IDSEmailServiceFactory
{
    public IDSEmailService Create(string serviceKey)
    {
        var service = serviceProvider.GetKeyedService<IDSEmailService>(serviceKey);

        if (service is null) throw new ArgumentException($"No DSEmailService with the key '{serviceKey}' exists.");
        
        return service;
    }
}
