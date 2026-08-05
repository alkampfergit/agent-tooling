namespace Agent.Tools.Tests.Smtp;

using global::Smtp.Configuration;
using global::Smtp.Services;
using Microsoft.Graph;
using Microsoft.Kiota.Abstractions;
using Moq;

[TestFixture]
[Category("smtp")]
[Category("Unit")]
public class GraphEmailServiceUnitTests
{
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

    [Test]
    public void Constructor_WithInjectedClientFactory_DoesNotThrow()
    {
        var adapter = new Mock<IRequestAdapter>();
        var client = new GraphServiceClient(adapter.Object);

        Assert.DoesNotThrow(() => new GraphEmailService(CreateServer(), new HtmlToTextConverter(), _ => client));
    }
}
