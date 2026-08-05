namespace Agent.Tools.Tests.Smtp;

using global::Smtp.Configuration;

[TestFixture]
[Category("smtp")]
[Category("Unit")]
public class ServerConfigValidationTests
{
    [Test]
    public void Validate_ImapWithAllFields_DoesNotThrow()
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

        Assert.DoesNotThrow(() => server.Validate());
    }

    [Test]
    public void Validate_ImapDefaultTypeWithAllFields_DoesNotThrow()
    {
        var server = new ServerConfig
        {
            Name = "primary",
            Address = "imap.example.com",
            Port = 993,
            Username = "user",
            Password = "pass",
            UseHttps = true
        };

        Assert.That(server.Type, Is.EqualTo("Imap"));
        Assert.DoesNotThrow(() => server.Validate());
    }

    [Test]
    public void Validate_ImapMissingFields_ThrowsWithFieldNames()
    {
        var server = new ServerConfig { Name = "primary", Type = "Imap" };

        var ex = Assert.Throws<InvalidOperationException>(() => server.Validate());
        Assert.Multiple(() =>
        {
            Assert.That(ex!.Message, Does.Contain("configured"));
            Assert.That(ex.Message, Does.Contain("Address"));
            Assert.That(ex.Message, Does.Contain("Port"));
            Assert.That(ex.Message, Does.Contain("Username"));
            Assert.That(ex.Message, Does.Contain("Password"));
            Assert.That(ex.Message, Does.Contain("UseHttps"));
        });
    }

    [Test]
    public void Validate_Office365WithClientIdAndUsername_DoesNotThrow()
    {
        var server = new ServerConfig
        {
            Name = "o365",
            Type = "Office365",
            Username = "user@contoso.com",
            ClientId = "11111111-1111-1111-1111-111111111111"
        };

        Assert.DoesNotThrow(() => server.Validate());
        Assert.That(server.TenantId, Is.EqualTo("common"));
    }

    [Test]
    public void Validate_Office365MissingClientId_Throws()
    {
        var server = new ServerConfig { Name = "o365", Type = "Office365", Username = "user@contoso.com" };

        var ex = Assert.Throws<InvalidOperationException>(() => server.Validate());
        Assert.That(ex!.Message, Does.Contain("ClientId"));
    }

    [Test]
    public void Validate_Office365MissingUsername_Throws()
    {
        var server = new ServerConfig { Name = "o365", Type = "Office365", ClientId = "client-id" };

        var ex = Assert.Throws<InvalidOperationException>(() => server.Validate());
        Assert.That(ex!.Message, Does.Contain("Username"));
    }

    [Test]
    public void Validate_Office365WithImapFieldSet_Throws()
    {
        var server = new ServerConfig
        {
            Name = "o365",
            Type = "Office365",
            Username = "user@contoso.com",
            ClientId = "client-id",
            Address = "imap.example.com"
        };

        var ex = Assert.Throws<InvalidOperationException>(() => server.Validate());
        Assert.Multiple(() =>
        {
            Assert.That(ex!.Message, Does.Contain("Address"));
            Assert.That(ex.Message, Does.Contain("must not be set"));
        });
    }

    [Test]
    public void Validate_UnknownType_Throws()
    {
        var server = new ServerConfig { Name = "weird", Type = "Pop3" };

        var ex = Assert.Throws<InvalidOperationException>(() => server.Validate());
        Assert.Multiple(() =>
        {
            Assert.That(ex!.Message, Does.Contain("Pop3"));
            Assert.That(ex.Message, Does.Contain("configured"));
        });
    }
}
