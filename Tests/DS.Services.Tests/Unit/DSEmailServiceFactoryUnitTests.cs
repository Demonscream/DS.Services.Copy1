using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.

namespace DS.Services.Tests.Unit;

[TestFixture, ExcludeFromCodeCoverage]
public class DSEmailServiceFactoryUnitTests
{
    private Dictionary<string, string> _configurationData;
    private IConfigurationRoot _configuration;
    private DSEmailServiceFactory _factory;
    
    private IOptions<DSEmailServiceOptions> _options;
    private ServiceCollection _services;
    private Mock<ILogger<DSEmailService>> _mockLogger;

    [SetUp]
    public void TestSetup()
    {
        _mockLogger = new();
        _options = Options.Create(new DSEmailServiceOptions());

        _configurationData = new Dictionary<string, string>
        {
            { "DSEmailServiceConfig:0:ServiceKey", "service0" },
            { "DSEmailServiceConfig:0:SmtpServer", "server0" },
            { "DSEmailServiceConfig:0:UserName", "username0" },
            { "DSEmailServiceConfig:0:Password", "password0" },
            { "DSEmailServiceConfig:0:From", "from0" },
            { "DSEmailServiceConfig:0:FromName", "from_name0" },
            { "DSEmailServiceConfig:0:BccEmailAddresses:0:", "bcc_email00" },
            { "DSEmailServiceConfig:0:BccEmailAddresses:1:", "bcc_email01" }
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(_configurationData)
            .Build();

        _services = new ServiceCollection();
        _services.AddSingleton(_mockLogger.Object);
        _services.AddDsEmailService(_configuration.GetSection("DSEmailServiceConfig"), null);
        var serviceProvider = _services.BuildServiceProvider();
        _factory = new DSEmailServiceFactory(serviceProvider);


    }

    [Test]
    public void ReturnsRequiredService()
    {
        var serviceKey = "service0";

        var requiredService = _factory.Create(serviceKey);

        requiredService.Should().BeOfType<DSEmailService>();
    }

    [Test]
    public void ThrowsExceptionWhenServiceDoesNotExist()
    {
        var serviceKey = "service1";

        var act = () => _factory.Create(serviceKey);

        act.Should().Throw<ArgumentException>().WithMessage("No DSEmailService with the key 'service1' exists.");
    }
}

#pragma warning restore CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.

