namespace Agent.Tools.Tests.Smtp;

using global::Smtp.Configuration;
using global::Smtp.Services;
using global::Smtp.Models;

[Explicit("Requires mailtrap or similar IMAP test mailbox to be running")]
public class EmailServiceIntegrationTests
{
    private ServerConfig? _testServer;

    [OneTimeSetUp]
    public void SetUp()
    {
        try
        {
            var provider = new ConfigurationProvider();
            _testServer = provider.GetServer("test");
        }
        catch
        {
            Assert.Ignore("No test mailbox configured with name 'test'");
        }
    }

    [Test]
    public async Task GetUnreadEmailsAsync_ReturnsValidList()
    {
        if (_testServer == null) Assert.Ignore("No test server");

        var converter = new HtmlToTextConverter();
        var service = new EmailService(_testServer, converter);
        var emails = await service.GetUnreadEmailsAsync();

        Assert.That(emails, Is.Not.Null);
        Assert.That(emails, Is.TypeOf<List<EmailSummary>>());
        Assert.That(emails, Is.Not.Empty, "Test mailbox should have unread emails");

        var first = emails[0];
        Assert.That(first.Id, Is.Not.Null.And.Not.Empty);
        Assert.That(first.From, Is.Not.Null.And.Not.Empty);
        Assert.That(first.Subject, Is.Not.Null);
        Assert.That(first.Date, Is.Not.EqualTo(DateTime.MinValue));
        Assert.That(first.Preview, Is.Not.Null);
    }

    [Test]
    public async Task GetEmailDetailsAsync_ReturnsFullEmail()
    {
        if (_testServer == null) Assert.Ignore("No test server");

        var converter = new HtmlToTextConverter();
        var service = new EmailService(_testServer, converter);
        var emails = await service.GetUnreadEmailsAsync();

        if (!emails.Any()) Assert.Ignore("No unread emails in test mailbox");

        var firstEmailId = emails[0].Id;
        var details = await service.GetEmailDetailsAsync(firstEmailId);

        Assert.That(details, Is.Not.Null);
        Assert.That(details!.Id, Is.EqualTo(firstEmailId));
        Assert.That(details.From, Is.Not.Null.And.Not.Empty);
        Assert.That(details.Subject, Is.Not.Null);
        Assert.That(details.Body, Is.Not.Null);
        Assert.That(details.Date, Is.Not.EqualTo(DateTime.MinValue));
    }

    [Test]
    public async Task GetEmailDetailsAsync_WithInvalidId_ReturnsNull()
    {
        if (_testServer == null) Assert.Ignore("No test server");

        var converter = new HtmlToTextConverter();
        var service = new EmailService(_testServer, converter);
        var details = await service.GetEmailDetailsAsync("999999");

        Assert.That(details, Is.Null);
    }

    [Test]
    public async Task MarkEmailAsReadAsync_WithValidId_ReturnsTrue()
    {
        if (_testServer == null) Assert.Ignore("No test server");

        var converter = new HtmlToTextConverter();
        var service = new EmailService(_testServer, converter);

        var unreadBefore = await service.GetUnreadEmailsAsync();
        if (!unreadBefore.Any()) Assert.Ignore("No unread emails to mark as read");

        var emailId = unreadBefore[0].Id;
        var result = await service.MarkEmailAsReadAsync(emailId);

        Assert.That(result, Is.True);

        var unreadAfter = await service.GetUnreadEmailsAsync();
        Assert.That(unreadAfter.Count, Is.LessThan(unreadBefore.Count));
    }

    [Test]
    public async Task MarkEmailAsReadAsync_WithInvalidId_ReturnsFalse()
    {
        if (_testServer == null) Assert.Ignore("No test server");

        var converter = new HtmlToTextConverter();
        var service = new EmailService(_testServer, converter);
        var result = await service.MarkEmailAsReadAsync("999999");

        Assert.That(result, Is.False);
    }
}
