namespace Agent.Tools.Tests.Smtp;

using global::Smtp.Configuration;
using global::Smtp.Services;
using global::Smtp.Models;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

[TestFixture]
[Category("smtp")]
[Category("Integration")]
public class ImapEmailServiceIntegrationTests
{
    private ServerConfig? _testServer;
    private SmtpClient? _smtpClient;

    [OneTimeSetUp]
    public async Task SetUp()
    {
        _testServer = new ServerConfig
        {
            Name = "mailtrap",
            Address = "localhost",
            Port = 9143,
            Username = "mailtrap",
            Password = "mailtrap",
            UseHttps = false
        };

        _smtpClient = new SmtpClient();
        _smtpClient.Connect("localhost", 9025, SecureSocketOptions.None);
        _smtpClient.Authenticate("mailtrap", "mailtrap");

        var message1 = CreateMessage("test1@example.com", "Test Email 1", "This is the first test email");
        var message2 = CreateMessage("test2@example.com", "Test Email 2", "This is the second test email");
        var message3 = CreateMessage("test3@example.com", "Test Email 3 with HTML", "<html><body><h1>HTML Test</h1></body></html>");

        await _smtpClient.SendAsync(message1);
        await _smtpClient.SendAsync(message2);
        await _smtpClient.SendAsync(message3);
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
        _smtpClient?.Disconnect(true);
        _smtpClient?.Dispose();
    }

    private static MimeMessage CreateMessage(string recipient, string subject, string body)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Test Sender", "sender@example.com"));
        message.To.Add(new MailboxAddress("Test Recipient", recipient));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };
        return message;
    }

    [Test]
    public async Task GetUnreadEmailsAsync_ReturnsValidList()
    {
        if (_testServer == null) Assert.Ignore("No test server");

        var service = new ImapEmailService(_testServer!);
        var emails = await service.GetUnreadEmailsAsync();

        Assert.That(emails, Is.Not.Null);
        Assert.That(emails, Is.TypeOf<List<EmailSummary>>());
        Assert.That(emails, Is.Not.Empty, "Test mailbox should have unread emails");

        var first = emails[0];
        using (Assert.EnterMultipleScope())
        {
            Assert.That(first.Id, Is.Not.Null.And.Not.Empty);
            Assert.That(first.From, Is.Not.Null.And.Not.Empty);
            Assert.That(first.Subject, Is.Not.Null);
            Assert.That(first.Date, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(first.Preview, Is.Not.Null);
        }
    }

    [Test]
    public async Task GetEmailDetailsAsync_ReturnsFullEmail()
    {
        if (_testServer == null) Assert.Ignore("No test server");

        var service = new ImapEmailService(_testServer!);
        var emails = await service.GetUnreadEmailsAsync();

        if (emails.Count == 0) Assert.Ignore("No unread emails in test mailbox");

        var firstEmailId = emails[0].Id;
        var details = await service.GetEmailDetailsAsync(firstEmailId);

        Assert.That(details, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(details!.Id, Is.EqualTo(firstEmailId));
            Assert.That(details.From, Is.Not.Null.And.Not.Empty);
            Assert.That(details.Subject, Is.Not.Null);
            Assert.That(details.Body, Is.Not.Null);
            Assert.That(details.Date, Is.Not.EqualTo(DateTime.MinValue));
        }
    }

    [Test]
    public async Task GetEmailDetailsAsync_WithInvalidId_ReturnsNull()
    {
        if (_testServer == null) Assert.Ignore("No test server");

        var service = new ImapEmailService(_testServer!);
        var details = await service.GetEmailDetailsAsync("999999");

        Assert.That(details, Is.Null);
    }

    [Test]
    public async Task MarkEmailAsReadAsync_WithValidId_ReturnsTrue()
    {
        if (_testServer == null) Assert.Ignore("No test server");

        var service = new ImapEmailService(_testServer!);

        var unreadBefore = await service.GetUnreadEmailsAsync();
        if (unreadBefore.Count == 0) Assert.Ignore("No unread emails to mark as read");

        var emailId = unreadBefore[0].Id;
        var result = await service.MarkEmailAsReadAsync(emailId);

        Assert.That(result, Is.True);

        var unreadAfter = await service.GetUnreadEmailsAsync();
        Assert.That(unreadAfter, Has.Count.LessThan(unreadBefore.Count));
    }

    [Test]
    public async Task MarkEmailAsReadAsync_WithInvalidId_ReturnsFalse()
    {
        if (_testServer == null) Assert.Ignore("No test server");

        var service = new ImapEmailService(_testServer!);
        var result = await service.MarkEmailAsReadAsync("999999");

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task MarkEmailsAsReadAsync_WithMultipleValidIds_ReturnsAllSuccess()
    {
        if (_testServer == null) Assert.Ignore("No test server");

        var service = new ImapEmailService(_testServer!);

        var unreadBefore = await service.GetUnreadEmailsAsync();
        if (unreadBefore.Count < 2) Assert.Ignore("Not enough unread emails to mark as read");

        var ids = unreadBefore.Take(2).Select(e => e.Id).ToList();
        var results = await service.MarkEmailsAsReadAsync(ids);

        Assert.That(results, Has.Count.EqualTo(2));
        Assert.That(results.All(r => r.Success), Is.True);

        var unreadAfter = await service.GetUnreadEmailsAsync();
        Assert.That(unreadAfter, Has.Count.EqualTo(unreadBefore.Count - 2));
    }

    [Test]
    public async Task MarkEmailsAsReadAsync_WithMixOfValidAndInvalidIds_ReturnsPartialFailure()
    {
        if (_testServer == null) Assert.Ignore("No test server");

        var service = new ImapEmailService(_testServer!);

        var unreadBefore = await service.GetUnreadEmailsAsync();
        if (unreadBefore.Count == 0) Assert.Ignore("No unread emails to mark as read");

        var validId = unreadBefore[0].Id;
        var results = await service.MarkEmailsAsReadAsync(new[] { validId, "999999" });

        Assert.That(results, Has.Count.EqualTo(2));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(results.Single(r => r.Id == validId).Success, Is.True);
            Assert.That(results.Single(r => r.Id == "999999").Success, Is.False);
        }
    }

    private static readonly string[] NonNumericIds = { "abc" };

    [Test]
    public async Task MarkEmailsAsReadAsync_WithNonNumericId_ReturnsFalseNotException()
    {
        if (_testServer == null) Assert.Ignore("No test server");

        var service = new ImapEmailService(_testServer!);

        var results = await service.MarkEmailsAsReadAsync(NonNumericIds);

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].Success, Is.False);
    }
}
