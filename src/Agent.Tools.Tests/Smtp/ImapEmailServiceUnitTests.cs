namespace Agent.Tools.Tests.Smtp;

using global::Smtp.Configuration;
using global::Smtp.Services;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MimeKit;
using Moq;

[TestFixture]
[Category("smtp")]
[Category("Unit")]
public class ImapEmailServiceUnitTests
{
    private static readonly string[] SingleValidId = { "42" };
    private static readonly string[] SingleNonNumericId = { "abc" };
    private static readonly string[] SingleMissingId = { "999999" };
    private static readonly string[] MixedIds = { "1", "2", "abc" };

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

    private static (ImapEmailService Service, Mock<IMailFolder> Inbox) CreateServiceWithMockInbox()
    {
        var inbox = new Mock<IMailFolder>();
        inbox.Setup(f => f.OpenAsync(FolderAccess.ReadWrite, It.IsAny<CancellationToken>())).ReturnsAsync(FolderAccess.ReadWrite);
        inbox.Setup(f => f.OpenAsync(FolderAccess.ReadOnly, It.IsAny<CancellationToken>())).ReturnsAsync(FolderAccess.ReadOnly);

        var client = new Mock<IImapClient>();
        client.SetupGet(c => c.Inbox).Returns(inbox.Object);

        var service = new ImapEmailService(CreateServer(), new HtmlToTextConverter(), _ => client.Object);
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

        var results = await service.MarkEmailsAsReadAsync(SingleValidId);

        Assert.That(results, Has.Count.EqualTo(1));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(results[0].Id, Is.EqualTo("42"));
            Assert.That(results[0].Success, Is.True);
        }
        inbox.Verify(f => f.Store(uid, It.IsAny<IStoreFlagsRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task MarkEmailsAsReadAsync_WithNonNumericId_ReturnsFalseWithoutTouchingInbox()
    {
        var (service, inbox) = CreateServiceWithMockInbox();

        var results = await service.MarkEmailsAsReadAsync(SingleNonNumericId);

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

        var results = await service.MarkEmailsAsReadAsync(SingleMissingId);

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].Success, Is.False);
    }

    [Test]
    public void Constructor_WithDefaultClientFactory_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => new ImapEmailService(CreateServer(), new HtmlToTextConverter()));
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

        var results = await service.MarkEmailsAsReadAsync(MixedIds);

        Assert.That(results, Has.Count.EqualTo(3));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(results.Single(r => r.Id == "1").Success, Is.True);
            Assert.That(results.Single(r => r.Id == "2").Success, Is.False);
            Assert.That(results.Single(r => r.Id == "abc").Success, Is.False);
        }
        inbox.Verify(f => f.OpenAsync(FolderAccess.ReadWrite, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetUnreadEmailsAsync_ReturnsMappedSummaries()
    {
        var (service, inbox) = CreateServiceWithMockInbox();
        var uid = new UniqueId(7);
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Sender", "sender@example.com"));
        message.Subject = "Hello";
        message.Body = new TextPart("plain") { Text = "Body text" };

        inbox.Setup(f => f.SearchAsync(It.IsAny<SearchQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UniqueId> { uid });
        inbox.Setup(f => f.GetMessageAsync(uid, It.IsAny<CancellationToken>(), null)).ReturnsAsync(message);

        var summaries = await service.GetUnreadEmailsAsync();

        Assert.That(summaries, Has.Count.EqualTo(1));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(summaries[0].Id, Is.EqualTo(uid.ToString()));
            Assert.That(summaries[0].Subject, Is.EqualTo("Hello"));
            Assert.That(summaries[0].Preview, Does.Contain("Body text"));
        }
        inbox.Verify(f => f.OpenAsync(FolderAccess.ReadOnly, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetUnreadEmailsAsync_WithNoUnreadMessages_ReturnsEmptyList()
    {
        var (service, inbox) = CreateServiceWithMockInbox();

        inbox.Setup(f => f.SearchAsync(It.IsAny<SearchQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UniqueId>());

        var summaries = await service.GetUnreadEmailsAsync();

        Assert.That(summaries, Is.Empty);
    }

    [Test]
    public async Task GetEmailDetailsAsync_WithValidId_ReturnsMappedDetails()
    {
        var (service, inbox) = CreateServiceWithMockInbox();
        var uid = new UniqueId(42);
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Sender", "sender@example.com"));
        message.To.Add(new MailboxAddress("Recipient", "recipient@example.com"));
        message.Cc.Add(new MailboxAddress("CcPerson", "cc@example.com"));
        message.Subject = "Details subject";
        message.Body = new TextPart("plain") { Text = "Plain body" };

        inbox.Setup(f => f.GetMessageAsync(uid, It.IsAny<CancellationToken>(), null)).ReturnsAsync(message);

        var details = await service.GetEmailDetailsAsync("42");

        Assert.That(details, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(details!.Id, Is.EqualTo("42"));
            Assert.That(details.Subject, Is.EqualTo("Details subject"));
            Assert.That(details.To, Has.Count.EqualTo(1));
            Assert.That(details.Cc, Has.Count.EqualTo(1));
            Assert.That(details.Body, Is.EqualTo("Plain body"));
        }
    }

    [Test]
    public async Task GetEmailDetailsAsync_WithHtmlBody_ConvertsToPlainText()
    {
        var (service, inbox) = CreateServiceWithMockInbox();
        var uid = new UniqueId(43);
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Sender", "sender@example.com"));
        message.Subject = "Html subject";
        message.Body = new TextPart("html") { Text = "<p>Hello <b>world</b></p>" };

        inbox.Setup(f => f.GetMessageAsync(uid, It.IsAny<CancellationToken>(), null)).ReturnsAsync(message);

        var details = await service.GetEmailDetailsAsync("43");

        Assert.That(details, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(details!.Body, Does.Not.Contain("<p>"));
            Assert.That(details.Body, Does.Contain("Hello world"));
        }
    }

    [Test]
    public async Task GetEmailDetailsAsync_WithMessageNotFound_ReturnsNull()
    {
        var (service, inbox) = CreateServiceWithMockInbox();
        var uid = new UniqueId(999999);

        inbox.Setup(f => f.GetMessageAsync(uid, It.IsAny<CancellationToken>(), null))
            .ThrowsAsync(new MessageNotFoundException("not found"));

        var details = await service.GetEmailDetailsAsync("999999");

        Assert.That(details, Is.Null);
    }

    [Test]
    public void GetEmailDetailsAsync_WithInvalidFormat_ThrowsInvalidOperationException()
    {
        var (service, _) = CreateServiceWithMockInbox();

        Assert.ThrowsAsync<InvalidOperationException>(() => service.GetEmailDetailsAsync("abc"));
    }
}
