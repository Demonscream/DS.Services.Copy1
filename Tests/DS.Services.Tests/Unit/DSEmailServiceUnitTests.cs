using System.Net.Mail;
using Castle.Core.Logging;
using FluentAssertions;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Moq;
using Org.BouncyCastle.Crypto.Macs;

namespace DS.Services.Tests.Unit;

[TestFixture, ExcludeFromCodeCoverage]
public class DSEmailServiceUnitTests
{
    private DSEmailService _service;
    private Mock<ILogger<DSEmailService>> _loggerMock;
    private IOptions<DSEmailServiceOptions> _options;
    private DSEmailServiceOptions _serviceOptions;
    private Mock<ISmtpClient> _smtpClientMock;
    private EmailRequest _emailRequest;

    [SetUp]
    public void Setup()
    {
        _loggerMock = new();
        _smtpClientMock = new ();

        _serviceOptions = new DSEmailServiceOptions
        {
            CcEmailAddresses = ["cc1@test.com", "cc2@test.com"],
            BccEmailAddresses = ["bcc1@test.com", "bss2@test.com"],
            From = "from@test.com",
            FromName = "from name",
            Password = "password",
            SmtpServer = "mail.server.com",
            UserName = "username"
        };
        _emailRequest = new EmailRequest()
        {
            ToEmails = ["to@test.com"],
            Subject = "subject",
            HtmlBody = "<p>Test html body</p>",
            TextBody = "Test text body"
        };
        _options = Options.Create(_serviceOptions);

        _service = new DSEmailService(_loggerMock.Object, _options, _smtpClientMock.Object);
    }

    [TearDown]
    public void Teardown()
    {
        _service.Dispose();
    }

    [Test]
    public async Task SendAsync_WithValidData()
    {
       await _service.SendAsync(_emailRequest);

       _smtpClientMock.Verify(x => x.ConnectAsync(
           It.Is<string>(p1 => p1 == _serviceOptions.SmtpServer), 0, SecureSocketOptions.None, CancellationToken.None), Times.Once());
       _smtpClientMock.Verify(x => x.AuthenticateAsync(
           It.Is<string>(p1 => p1 == _serviceOptions.UserName),
           It.Is<string>(p2 => p2 == _serviceOptions.Password),
           CancellationToken.None), Times.Once());
       _smtpClientMock.Verify(x => x.SendAsync(
           It.Is<MimeMessage>(
               p1 => p1.Subject == _emailRequest.Subject
               && p1.HtmlBody == _emailRequest.HtmlBody
               && p1.TextBody == _emailRequest.TextBody), CancellationToken.None, null), Times.Once());
    }

    [Test]
    public async Task MergesAndDeduplicatesEmailCollections()
    {
        _emailRequest.CcEmails = ["cc2@test.com", "cc3@test.com"];

        await _service.SendAsync(_emailRequest);

        _smtpClientMock.Verify(x => x.SendAsync(
            It.Is<MimeMessage>(
                p1 => p1.Cc.Mailboxes.Count() == 3
                      && p1.Cc.Mailboxes.Any(m1 => m1.Address == "cc1@test.com")
                      && p1.Cc.Mailboxes.Any(m2 => m2.Address == "cc2@test.com")
                      && p1.Cc.Mailboxes.Any(m3 => m3.Address == "cc3@test.com"))
            , CancellationToken.None, null), Times.Once());
    }

    [Test]
    public async Task ProcessesAttachmentsFileStreamCorrectly()
    {
        _emailRequest.Attachments = [new()
        {
            FileName = "testFile1.pdf",
            FileStream = new MemoryStream()
        }];

        await _service.SendAsync(_emailRequest);

        _smtpClientMock.Verify(x => x.SendAsync(
            It.Is<MimeMessage>(
                p1 =>
                    p1.Attachments.Count() == 1 &&
                    p1.Attachments.First().ContentType.MimeType == "application/pdf" &&
                    p1.Attachments.First().ContentDisposition.FileName == "testFile1.pdf")
            , CancellationToken.None, null), Times.Once());
    }

    [Test]
    public async Task StreamAttachmentWithNoFileNameThrowsException()
    {
        _emailRequest.Attachments = [new()
        {
            FileStream = new MemoryStream()
        }];

        Func<Task> act = async () => { await _service.SendAsync(_emailRequest); };

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("FileName is required when using FileStream");

    }

    [Test]
    public async Task ProcessesAttachmentsFileStreamCorrectlyWithContentType()
    {
        _emailRequest.Attachments = [new()
        {
            FileName = "testFile1.pdf",
            FileStream = new MemoryStream(),
            ContentType = "application/json"

        }];

        await _service.SendAsync(_emailRequest);

        _smtpClientMock.Verify(x => x.SendAsync(
            It.Is<MimeMessage>(
                p1 =>
                    p1.Attachments.Count() == 1 &&
                    p1.Attachments.First().ContentType.MimeType == "application/json" &&
                    p1.Attachments.First().ContentDisposition.FileName == "testFile1.pdf")
            , CancellationToken.None, null), Times.Once());
    }

    [Test]
    public async Task ProcessesAttachmentsByteArrayCorrectly()
    {
        _emailRequest.Attachments = [new()
        {
            FileName = "testFile1.txt",
            FileBytes = "byte array"u8.ToArray()
        }];

        await _service.SendAsync(_emailRequest);

        _smtpClientMock.Verify(x => x.SendAsync(
            It.Is<MimeMessage>(
                p1 =>
                    p1.Attachments.Count() == 1 &&
                    p1.Attachments.First().ContentType.MimeType == "text/plain" &&
                    p1.Attachments.First().ContentDisposition.FileName == "testFile1.txt")
            , CancellationToken.None, null), Times.Once());
    }

    [Test]
    public async Task ByteArrayAttachmentWithNoFileNameThrowsException()
    {
        _emailRequest.Attachments = [new()
        {
            FileBytes = "byte array"u8.ToArray()
        }];

        Func<Task> act = async () => { await _service.SendAsync(_emailRequest); };

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("FileName is required when using FileBytes");

    }

    [Test]
    public async Task FileAttachmentWithNoFileFoundThrowsException()
    {
        _emailRequest.Attachments = [ new()
        {
            FilePath = "C:\\\\some\\path\\file.txt"
        }];

        Func<Task> act = async () => { await _service.SendAsync(_emailRequest); };

        await act.Should().ThrowAsync<FileNotFoundException>().WithMessage("File not found: C:\\\\some\\path\\file.txt");

    }

    [Test]
    public async Task FileAttachmentWithNoPathBytesOrStreamThrowsException()
    {
        _emailRequest.Attachments = [ new() ];

        Func<Task> act = async () => { await _service.SendAsync(_emailRequest); };

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Attachment must have either FilePath, FileBytes, or FileStream");

    }

    [Test]
    public async Task SendAsyncHandlesThrownException()
    {
        _smtpClientMock
            .Setup(x => x.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()))
            .ThrowsAsync(new InvalidOperationException("Test exception"));

        Func<Task> act = async () => { await _service.SendAsync(_emailRequest); };

        await act.Should().ThrowAsync<InvalidOperationException>();

    }
}