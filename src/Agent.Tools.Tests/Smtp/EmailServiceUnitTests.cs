namespace Agent.Tools.Tests.Smtp;

using global::Smtp.Configuration;
using global::Smtp.Services;
using MailKit;
using MailKit.Net.Imap;
using Moq;

[TestFixture]
[Category("smtp")]
[Category("Unit")]
public class EmailServiceUnitTests
{
    private static ServerConfig CreateServer()
    {
        return new ServerConfig
        {
            Name = "test",
            Address = "imap.example.com",
            Port = 993,
            Username = "user",
            Password = "pass",
            UseHttps = true
        };
    }

    private static (EmailService Service, Mock<IMailFolder> Inbox) CreateServiceWithMockInbox()
    {
        var inbox = new Mock<IMailFolder>();
        inbox.Setup(f => f.Open(FolderAccess.ReadWrite, It.IsAny<CancellationToken>()));

        var client = new Mock<IImapClient>();
        client.SetupGet(c => c.Inbox).Returns(inbox.Object);

        var service = new EmailService(CreateServer(), new HtmlToTextConverter(), _ => client.Object);
        return (service, inbox);
    }

    [Test]
    public async Task MarkEmailsAsReadAsync_WithValidId_SetsSeenFlagAndReturnsSuccess()
    {
        var (service, inbox) = CreateServiceWithMockInbox();
        var uid = new UniqueId(42);
        var message = new MimeKit.MimeMessage();

        inbox.Setup(f => f.GetMessage(uid, It.IsAny<CancellationToken>(), null)).Returns(message);
        inbox.Setup(f => f.Store(uid, It.IsAny<IStoreFlagsRequest>(), It.IsAny<CancellationToken>())).Returns(true);

        var results = await service.MarkEmailsAsReadAsync(new[] { "42" });

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(results[0].Id, Is.EqualTo("42"));
            Assert.That(results[0].Success, Is.True);
        });
        inbox.Verify(f => f.Store(uid, It.IsAny<IStoreFlagsRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task MarkEmailsAsReadAsync_WithNonNumericId_ReturnsFalseWithoutTouchingInbox()
    {
        var (service, inbox) = CreateServiceWithMockInbox();

        var results = await service.MarkEmailsAsReadAsync(new[] { "abc" });

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].Success, Is.False);
        inbox.Verify(f => f.GetMessage(It.IsAny<UniqueId>(), It.IsAny<CancellationToken>(), null), Times.Never);
    }

    [Test]
    public async Task MarkEmailsAsReadAsync_WithMessageNotFound_ReturnsFalse()
    {
        var (service, inbox) = CreateServiceWithMockInbox();
        var uid = new UniqueId(999999);

        inbox.Setup(f => f.GetMessage(uid, It.IsAny<CancellationToken>(), null))
            .Throws(new MessageNotFoundException("not found"));

        var results = await service.MarkEmailsAsReadAsync(new[] { "999999" });

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].Success, Is.False);
    }

    [Test]
    public void Constructor_WithDefaultClientFactory_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => new EmailService(CreateServer(), new HtmlToTextConverter()));
    }

    [Test]
    public async Task MarkEmailAsReadAsync_WithValidId_ReturnsTrue()
    {
        var (service, inbox) = CreateServiceWithMockInbox();
        var uid = new UniqueId(42);

        inbox.Setup(f => f.GetMessage(uid, It.IsAny<CancellationToken>(), null)).Returns(new MimeKit.MimeMessage());
        inbox.Setup(f => f.Store(uid, It.IsAny<IStoreFlagsRequest>(), It.IsAny<CancellationToken>())).Returns(true);

        var result = await service.MarkEmailAsReadAsync("42");

        Assert.That(result, Is.True);
    }

    [Test]
    public void MarkEmailAsReadAsync_WithInvalidFormat_ThrowsInvalidOperationException()
    {
        var (service, _) = CreateServiceWithMockInbox();

        Assert.ThrowsAsync<InvalidOperationException>(() => service.MarkEmailAsReadAsync("abc"));
    }

    [Test]
    public async Task MarkEmailsAsReadAsync_WithMixOfIds_OpensInboxOnceAndReturnsPerIdResults()
    {
        var (service, inbox) = CreateServiceWithMockInbox();
        var validUid = new UniqueId(1);
        var missingUid = new UniqueId(2);

        inbox.Setup(f => f.GetMessage(validUid, It.IsAny<CancellationToken>(), null)).Returns(new MimeKit.MimeMessage());
        inbox.Setup(f => f.Store(validUid, It.IsAny<IStoreFlagsRequest>(), It.IsAny<CancellationToken>())).Returns(true);
        inbox.Setup(f => f.GetMessage(missingUid, It.IsAny<CancellationToken>(), null))
            .Throws(new MessageNotFoundException("not found"));

        var results = await service.MarkEmailsAsReadAsync(new[] { "1", "2", "abc" });

        Assert.That(results, Has.Count.EqualTo(3));
        Assert.Multiple(() =>
        {
            Assert.That(results.Single(r => r.Id == "1").Success, Is.True);
            Assert.That(results.Single(r => r.Id == "2").Success, Is.False);
            Assert.That(results.Single(r => r.Id == "abc").Success, Is.False);
        });
        inbox.Verify(f => f.Open(FolderAccess.ReadWrite, It.IsAny<CancellationToken>()), Times.Once);
    }
}
