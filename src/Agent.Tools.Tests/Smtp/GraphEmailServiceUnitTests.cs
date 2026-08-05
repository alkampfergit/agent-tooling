namespace Agent.Tools.Tests.Smtp;

using Azure.Identity;
using global::Smtp.Configuration;
using global::Smtp.Services;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using Moq;

[TestFixture]
[Category("smtp")]
[Category("Unit")]
public class GraphEmailServiceUnitTests
{
    private static readonly string[] SingleId = { "1" };
    private static readonly string[] ExpectedTo = { "to@example.com" };
    private static readonly string[] ExpectedCc = { "cc@example.com" };

    private static ServerConfig CreateServer()
    {
        return new ServerConfig
        {
            Name = "o365",
            Type = "Office365",
            Username = "user@contoso.com",
            ClientId = "client-id"
        };
    }

    private static GraphEmailService CreateServiceWithFailingClientFactory(Exception exception)
    {
        return new GraphEmailService(CreateServer(), new HtmlToTextConverter(), _ => throw exception);
    }

    private static (GraphEmailService Service, Mock<IRequestAdapter> Adapter) CreateServiceWithMockAdapter()
    {
        var adapter = new Mock<IRequestAdapter>();
        adapter.SetupGet(a => a.BaseUrl).Returns("https://graph.microsoft.com/v1.0");
        var client = new GraphServiceClient(adapter.Object);
        var service = new GraphEmailService(CreateServer(), new HtmlToTextConverter(), _ => client);
        return (service, adapter);
    }

    [Test]
    public void Constructor_WithInjectedClientFactory_DoesNotThrow()
    {
        var adapter = new Mock<IRequestAdapter>();
        var client = new GraphServiceClient(adapter.Object);

        Assert.DoesNotThrow(() => new GraphEmailService(CreateServer(), new HtmlToTextConverter(), _ => client));
    }

    [Test]
    public void GetUnreadEmailsAsync_WhenCredentialUnavailable_WrapsWithClearMessage()
    {
        var service = CreateServiceWithFailingClientFactory(
            new CredentialUnavailableException("Persistence check failed."));

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => service.GetUnreadEmailsAsync());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex!.Message, Does.Contain("o365"));
            Assert.That(ex.Message, Does.Contain("gnome-keyring"));
            Assert.That(ex.Message, Does.Contain("kwallet"));
            Assert.That(ex.InnerException, Is.TypeOf<CredentialUnavailableException>());
        }
    }

    [Test]
    public void GetEmailDetailsAsync_WhenCredentialUnavailable_WrapsWithClearMessage()
    {
        var service = CreateServiceWithFailingClientFactory(
            new CredentialUnavailableException("Persistence check failed."));

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => service.GetEmailDetailsAsync("1"));

        Assert.That(ex!.Message, Does.Contain("o365"));
    }

    [Test]
    public void MarkEmailsAsReadAsync_WhenCredentialUnavailable_WrapsWithClearMessage()
    {
        var service = CreateServiceWithFailingClientFactory(
            new CredentialUnavailableException("Persistence check failed."));

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => service.MarkEmailsAsReadAsync(SingleId));

        Assert.That(ex!.Message, Does.Contain("o365"));
    }

    [Test]
    public void MarkEmailAsReadAsync_WhenCredentialUnavailable_WrapsWithClearMessage()
    {
        var service = CreateServiceWithFailingClientFactory(
            new CredentialUnavailableException("Persistence check failed."));

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => service.MarkEmailAsReadAsync("1"));

        Assert.That(ex!.Message, Does.Contain("o365"));
    }

    [Test]
    public void GetUnreadEmailsAsync_WhenClientFactoryThrowsOtherException_PropagatesUnwrapped()
    {
        var service = CreateServiceWithFailingClientFactory(new InvalidOperationException("boom"));

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => service.GetUnreadEmailsAsync());

        Assert.That(ex!.Message, Is.EqualTo("boom"));
    }

    [Test]
    public async Task GetUnreadEmailsAsync_ReturnsMappedSummaries()
    {
        var (service, adapter) = CreateServiceWithMockAdapter();
        var message = new Message
        {
            Id = "msg-1",
            From = new Recipient { EmailAddress = new EmailAddress { Address = "sender@example.com" } },
            Subject = "Hello",
            ReceivedDateTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            BodyPreview = "Preview text"
        };

        adapter.Setup(a => a.SendAsync<MessageCollectionResponse>(
                It.IsAny<RequestInformation>(),
                It.IsAny<ParsableFactory<MessageCollectionResponse>>(),
                It.IsAny<Dictionary<string, ParsableFactory<IParsable>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MessageCollectionResponse { Value = new List<Message> { message } });

        var summaries = await service.GetUnreadEmailsAsync();

        Assert.That(summaries, Has.Count.EqualTo(1));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(summaries[0].Id, Is.EqualTo("msg-1"));
            Assert.That(summaries[0].From, Is.EqualTo("sender@example.com"));
            Assert.That(summaries[0].Subject, Is.EqualTo("Hello"));
            Assert.That(summaries[0].Preview, Is.EqualTo("Preview text"));
        }
    }

    [Test]
    public async Task GetUnreadEmailsAsync_WithNullResponseValue_ReturnsEmptyList()
    {
        var (service, adapter) = CreateServiceWithMockAdapter();

        adapter.Setup(a => a.SendAsync<MessageCollectionResponse>(
                It.IsAny<RequestInformation>(),
                It.IsAny<ParsableFactory<MessageCollectionResponse>>(),
                It.IsAny<Dictionary<string, ParsableFactory<IParsable>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((MessageCollectionResponse?)null);

        var summaries = await service.GetUnreadEmailsAsync();

        Assert.That(summaries, Is.Empty);
    }

    [Test]
    public async Task GetEmailDetailsAsync_WithPlainTextBody_ReturnsMappedDetails()
    {
        var (service, adapter) = CreateServiceWithMockAdapter();
        var message = new Message
        {
            Id = "msg-2",
            From = new Recipient { EmailAddress = new EmailAddress { Address = "sender@example.com" } },
            ToRecipients = new List<Recipient> { new() { EmailAddress = new EmailAddress { Address = "to@example.com" } } },
            CcRecipients = new List<Recipient> { new() { EmailAddress = new EmailAddress { Address = "cc@example.com" } } },
            Subject = "Details",
            ReceivedDateTime = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero),
            Body = new ItemBody { ContentType = BodyType.Text, Content = "Plain body" }
        };

        adapter.Setup(a => a.SendAsync<Message>(
                It.IsAny<RequestInformation>(),
                It.IsAny<ParsableFactory<Message>>(),
                It.IsAny<Dictionary<string, ParsableFactory<IParsable>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);

        var details = await service.GetEmailDetailsAsync("msg-2");

        Assert.That(details, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(details!.Id, Is.EqualTo("msg-2"));
            Assert.That(details.Subject, Is.EqualTo("Details"));
            Assert.That(details.To, Is.EqualTo(ExpectedTo));
            Assert.That(details.Cc, Is.EqualTo(ExpectedCc));
            Assert.That(details.Body, Is.EqualTo("Plain body"));
        }
    }

    [Test]
    public async Task GetEmailDetailsAsync_WithNotFoundResponse_ReturnsNull()
    {
        var (service, adapter) = CreateServiceWithMockAdapter();

        adapter.Setup(a => a.SendAsync<Message>(
                It.IsAny<RequestInformation>(),
                It.IsAny<ParsableFactory<Message>>(),
                It.IsAny<Dictionary<string, ParsableFactory<IParsable>>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ODataError { ResponseStatusCode = 404 });

        var details = await service.GetEmailDetailsAsync("missing");

        Assert.That(details, Is.Null);
    }

    [Test]
    public void GetEmailDetailsAsync_WithNonNotFoundODataError_Propagates()
    {
        var (service, adapter) = CreateServiceWithMockAdapter();

        adapter.Setup(a => a.SendAsync<Message>(
                It.IsAny<RequestInformation>(),
                It.IsAny<ParsableFactory<Message>>(),
                It.IsAny<Dictionary<string, ParsableFactory<IParsable>>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ODataError { ResponseStatusCode = 500 });

        Assert.ThrowsAsync<ODataError>(() => service.GetEmailDetailsAsync("1"));
    }

    [Test]
    public async Task GetEmailDetailsAsync_WithHtmlBody_ConvertsToPlainText()
    {
        var (service, adapter) = CreateServiceWithMockAdapter();
        var message = new Message
        {
            Id = "msg-3",
            From = new Recipient { EmailAddress = new EmailAddress { Address = "sender@example.com" } },
            Subject = "Html",
            ReceivedDateTime = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero),
            Body = new ItemBody { ContentType = BodyType.Html, Content = "<p>Hello <b>world</b></p>" }
        };

        adapter.Setup(a => a.SendAsync<Message>(
                It.IsAny<RequestInformation>(),
                It.IsAny<ParsableFactory<Message>>(),
                It.IsAny<Dictionary<string, ParsableFactory<IParsable>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);

        var details = await service.GetEmailDetailsAsync("msg-3");

        Assert.That(details, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(details!.Body, Does.Not.Contain("<p>"));
            Assert.That(details.Body, Does.Contain("Hello world"));
        }
    }
}
