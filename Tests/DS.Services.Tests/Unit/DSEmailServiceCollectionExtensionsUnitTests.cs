using DS.Services.Email;
using FluentAssertions;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Moq;

#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
#pragma warning disable NUnit1032 // An IDisposable field/property should be Disposed in a TearDown method
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
#pragma warning disable CS0252 // Possible unintended reference comparison; left hand side needs cast

namespace DS.Services.Tests.Unit;

[TestFixture, ExcludeFromCodeCoverage]
public class DSEmailServiceCollectionExtensionsUnitTests
{
    private Dictionary<string, string> _configurationData;
    private IConfigurationRoot _configuration;
    private ServiceCollection _services;
    private Mock<ILogger<DSEmailService>> _mockLogger;
    private Mock<ILogger<Program>> _mockProgramLogger;
    private Mock<ISmtpClient> _mockSmtpClient;

    [SetUp]
    public void TestSetup()
    {
        _configurationData = new Dictionary<string, string>
        {
            { "DSEmailServiceConfig:0:ServiceKey", "service0" },
            { "DSEmailServiceConfig:0:SmtpServer", "server0" },
            { "DSEmailServiceConfig:0:UserName", "username0" },
            { "DSEmailServiceConfig:0:Password", "password0" },
            { "DSEmailServiceConfig:0:From", "from0" },
            { "DSEmailServiceConfig:0:FromName", "from_name0" },
            { "DSEmailServiceConfig:0:BccEmailAddresses:0:", "bcc_email00" },
            { "DSEmailServiceConfig:0:BccEmailAddresses:1:", "bcc_email01" },
            { "DSEmailServiceConfig:1:ServiceKey", "service1" },
            { "DSEmailServiceConfig:1:SmtpServer", "server1" },
            { "DSEmailServiceConfig:1:UserName", "username1" },
            { "DSEmailServiceConfig:1:Password", "password1" },
            { "DSEmailServiceConfig:1:From", "from1" },
            { "DSEmailServiceConfig:1:FromName", "from_name1" },
            { "DSEmailServiceConfig:1:BccEmailAddresses:0:", "bcc_email10" },
            { "DSEmailServiceConfig:1:BccEmailAddresses:1:", "bcc_email11" }
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(_configurationData)
            .Build();


        _services = new ServiceCollection();

        _mockLogger = new ();
        _mockProgramLogger = new ();
        _mockSmtpClient = new ();
        _services.AddSingleton(_mockLogger.Object);
    }

    [Test]
    public void CanResolveKeyedEmailServicesIndependently()
    {
        var preCount = _services.Count();
        _services.AddDsEmailService(_configuration.GetSection("DSEmailServiceConfig"), _mockProgramLogger.Object);
        var serviceProvider = _services.BuildServiceProvider();
        
        var service0 = serviceProvider.GetKeyedService<IDSEmailService>("service0");
        var service1 = serviceProvider.GetKeyedService<IDSEmailService>("service1");

        _services.Count().Should().Be(preCount + 4);
        _services.Should().Contain(x => x.ServiceType == typeof(ILogger<DSEmailService>));
        _services.Should().Contain(x => x.ServiceType == typeof(ISmtpClient));
        _services.Should().Contain(x => x.ServiceType == typeof(IDSEmailServiceFactory));
        _services.Should().Contain(x => x.ServiceType == typeof(IDSEmailService) && x.ServiceKey == "service0");
        _services.Should().Contain(x => x.ServiceType == typeof(IDSEmailService) && x.ServiceKey == "service1");

        service0.Should().NotBeNull();
        service1.Should().NotBeNull();
    }

    [Test]
    public void WillReturnNullWhenKeyIsNotMatched()
    {
        _services.AddDsEmailService(_configuration.GetSection("DSEmailServiceConfig"), _mockProgramLogger.Object);
        var serviceProvider = _services.BuildServiceProvider();

        var service99 = serviceProvider.GetKeyedService<IDSEmailService>("service99");

        service99.Should().BeNull();
    }

    [Test]
    public void ReturnsWithoutThrowingExceptionIfConfigListIsNull()
    {
        var services = _services.AddDsEmailService(_configuration.GetSection("EmptySection"), _mockProgramLogger.Object);
        var serviceProvider = _services.BuildServiceProvider();

        services.Should().BeSameAs(_services);
        _services.Count().Should().Be(1);
    }

    [Test]
    public void ReturnsWithoutThrowingExceptionIfNoServiceKey()
    {
        _configurationData = new Dictionary<string, string>
        {
            { "DSEmailServiceConfig:0:ServiceKey", "" }
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(_configurationData)
            .Build();

        _services.AddDsEmailService(_configuration.GetSection("DSEmailServiceConfig"), _mockProgramLogger.Object);
        var serviceProvider = _services.BuildServiceProvider();

        _services.Count().Should().Be(3);
        _services.Should().Contain(x => x.ServiceType == typeof(ILogger<DSEmailService>));
        _services.Should().Contain(x => x.ServiceType == typeof(ISmtpClient));
        _services.Should().Contain(x => x.ServiceType == typeof(IDSEmailServiceFactory));
    }
}

#pragma warning restore CS0252 // Possible unintended reference comparison; left hand side needs cast
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
#pragma warning restore NUnit1032 // An IDisposable field/property should be Disposed in a TearDown method
#pragma warning restore CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.