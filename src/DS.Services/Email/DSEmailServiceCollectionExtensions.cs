namespace DS.Services.Email;

/// <summary>
/// Extension methods for setting up the DSEmailService
/// </summary>
public static class DSEmailServiceCollectionExtensions
{
    public static IServiceCollection AddDsEmailService(
        this IServiceCollection services,
        IConfiguration dsConfigurationSection,
        ILogger logger) 
    {
            
        var emailConfigList = dsConfigurationSection.Get<List<DSEmailServiceOptions>>();

        if (emailConfigList is null)
        {
            return services;
        }

        services.TryAddTransient<ISmtpClient, SmtpClient>();
        services.TryAddTransient<IDSEmailServiceFactory, DSEmailServiceFactory>();

        foreach (var config in emailConfigList)
        {
            if (string.IsNullOrWhiteSpace(config.ServiceKey))
            {
                continue;
            }

            services.AddKeyedTransient<IDSEmailService>(config.ServiceKey, (serviceProvider, key) =>
            {
                var serviceLogger = serviceProvider.GetRequiredService<ILogger<DSEmailService>>();
                var smtpClient = serviceProvider.GetRequiredService<ISmtpClient>();
                var options = Options.Create(config);

                return new DSEmailService(serviceLogger, options, smtpClient);
            });
        }
        return services;
    }
}