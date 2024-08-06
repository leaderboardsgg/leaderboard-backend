using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LeaderboardBackend.Services;
using MailKit;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Moq;
using NUnit.Framework;

namespace LeaderboardBackend.Test.Features.Emails;

[Obsolete("EmailSender is no longer used.")]
[Ignore("EmailSender is no longer used.")]
public class EmailSenderTests
{
    private IEmailSender _sut = null!;
    private EmailSenderConfig _config = null!;
    private Mock<ISmtpClient> _smtpClientMock = null!;
    private Mock<ILogger<EmailSender>> _loggerMock = null!;

    [SetUp]
    public void SetUp()
    {
        Faker<EmailSenderConfig>? faker = new AutoFaker<EmailSenderConfig>()
            .RuleFor(x => x.SenderAddress, b => b.Internet.Email());

        _config = faker.Generate();
        _smtpClientMock = new Mock<ISmtpClient>();
        _loggerMock = new Mock<ILogger<EmailSender>>();
        _sut = new EmailSender(Options.Create(_config), _loggerMock.Object, _smtpClientMock.Object);
    }

    [Test]
    public async Task EnqueueEmailAsync_ShouldSendEmail()
    {
        string expectedRecipientAddress = new Bogus.DataSets.Internet().Email();
        const string EXPECTED_SUBJECT = "Account recovery";
        const string EXPECTED_MESSAGE = "Click <a href=\"https://youtu.be/kpk2tdsPh0A?t=638\">here</a> to recover your account.";
        MimeMessage? sentMessage = null;

        _smtpClientMock.Setup(x => x.SendAsync(It.IsAny<MimeMessage>(),
                It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()))
            .Callback((MimeMessage m, CancellationToken _, ITransferProgress _) => sentMessage = m);

        await _sut.EnqueueEmailAsync(expectedRecipientAddress, EXPECTED_SUBJECT, EXPECTED_MESSAGE);

        _smtpClientMock.Verify(x => x.SendAsync(It.IsAny<MimeMessage>(),
                It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()), Times.Once());
        sentMessage.Should().NotBeNull();

        // To
        sentMessage!.To.Mailboxes.Should().HaveCount(1);
        MailboxAddress recipientAddress = sentMessage.To.Mailboxes.Single();
        recipientAddress.Address.Should().Be(expectedRecipientAddress);
        recipientAddress.Name.Should().BeNull();

        // From
        sentMessage.From.Mailboxes.Should().HaveCount(1);
        MailboxAddress senderAddress = sentMessage.From.Mailboxes.Single();
        senderAddress.Address.Should().Be(_config.SenderAddress);
        senderAddress.Name.Should().Be(_config.SenderName);

        // Message
        sentMessage.Subject.Should().Be(EXPECTED_SUBJECT);
        sentMessage.HtmlBody.Should().Be(EXPECTED_MESSAGE);
    }

    [Test]
    public async Task EnqueueEmailAsync_ShouldConnectUsingConfig()
    {
        await EnqueueEmailAsync(_sut);

        _smtpClientMock.Verify(x =>
            x.ConnectAsync(_config.Smtp!.Host, _config.Smtp.Port, _config.Smtp.UseSsl, It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Test]
    public async Task EnqueueEmailAsync_ConfigWithCredentials_ShouldAuthenticate()
    {
        _config.Smtp!.Username = "username";
        _config.Smtp.Password = "password";

        await EnqueueEmailAsync(_sut);

        _smtpClientMock.Verify(x =>
            x.AuthenticateAsync(_config.Smtp!.Username, _config.Smtp.Password, It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Test]
    public async Task EnqueueEmailAsync_ConfigWithoutCredentials_ShouldNotAuthenticate()
    {
        _config.Smtp!.Username = null;
        _config.Smtp.Password = null;

        await EnqueueEmailAsync(_sut);

        _smtpClientMock.Verify(x =>
            x.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never());
    }

    [Test]
    public async Task EnqueueEmailAsync_SmtpConfigIsNull_ShouldNotThrow()
    {
        _config.Smtp = null;

        Func<Task> action = () => EnqueueEmailAsync(_sut);

        await action.Should().NotThrowAsync();
    }

    private static async Task EnqueueEmailAsync(IEmailSender sender)
    {
        string address = new Bogus.DataSets.Internet().Email();
        const string SUBJECT = "Account recovery";
        const string MESSAGE = "Click <a href=\"https://youtu.be/kpk2tdsPh0A?t=638\">here</a> to recover your account.";

        await sender.EnqueueEmailAsync(address, SUBJECT, MESSAGE);
    }
}
