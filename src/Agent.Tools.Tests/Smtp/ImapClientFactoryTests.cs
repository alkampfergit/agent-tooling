namespace Agent.Tools.Tests.Smtp;

using global::Smtp.Configuration;
using global::Smtp.Services;

[TestFixture]
[Category("smtp")]
[Category("Unit")]
public class ImapClientFactoryTests
{
    [Test]
    public void CreateClient_WithUnreachableServer_ThrowsInvalidOperationExceptionWithContext()
    {
        var server = new ServerConfig
        {
            Name = "unreachable",
            Type = "Imap",
            Address = "127.0.0.1",
            Port = 1,
            Username = "user",
            Password = "pass",
            UseHttps = false
        };

        var ex = Assert.Throws<InvalidOperationException>(() => ImapClientFactory.CreateClient(server));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex!.Message, Does.Contain("127.0.0.1"));
            Assert.That(ex.Message, Does.Contain("user"));
            Assert.That(ex.InnerException, Is.Not.Null);
        }
    }
}
