namespace Agent.Tools.Tests.Smtp;

using global::Smtp.Configuration;
using global::Smtp.Services;

[TestFixture]
[Category("smtp")]
[Category("Unit")]
public class EmailServiceFactoryTests
{
    [Test]
    public void Create_ImapType_ReturnsImapEmailService()
    {
        var server = new ServerConfig
        {
            Name = "primary",
            Type = "Imap",
            Address = "imap.example.com",
            Port = 993,
            Username = "user",
            Password = "pass",
            UseHttps = true
        };

        var service = EmailServiceFactory.Create(server);

        Assert.That(service, Is.TypeOf<ImapEmailService>());
    }

    [Test]
    public void Create_Office365Type_ReturnsGraphEmailService()
    {
        var server = new ServerConfig
        {
            Name = "o365",
            Type = "Office365",
            Username = "user@contoso.com",
            ClientId = "client-id"
        };

        var service = EmailServiceFactory.Create(server);

        Assert.That(service, Is.TypeOf<GraphEmailService>());
    }

    [Test]
    public void Create_TypeIsCaseInsensitive()
    {
        var server = new ServerConfig
        {
            Name = "o365",
            Type = "office365",
            Username = "user@contoso.com",
            ClientId = "client-id"
        };

        var service = EmailServiceFactory.Create(server);

        Assert.That(service, Is.TypeOf<GraphEmailService>());
    }

    [Test]
    public void Create_UnknownType_Throws()
    {
        var server = new ServerConfig { Name = "weird", Type = "Pop3" };

        var ex = Assert.Throws<InvalidOperationException>(() => EmailServiceFactory.Create(server));
        Assert.That(ex!.Message, Does.Contain("Pop3"));
    }
}
