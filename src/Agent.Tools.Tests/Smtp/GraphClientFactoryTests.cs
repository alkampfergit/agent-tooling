namespace Agent.Tools.Tests.Smtp;

using global::Smtp.Configuration;
using global::Smtp.Services;

[TestFixture]
[Category("smtp")]
[Category("Unit")]
public class GraphClientFactoryTests
{
    [Test]
    public void GetAuthRecordPath_IncludesServerNameAndIsUnderLocalAppData()
    {
        var server = new ServerConfig { Name = "nebula", Type = "Office365", Username = "u", ClientId = "c" };

        var path = GraphClientFactory.GetAuthRecordPath(server);

        var expectedBase = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(path, Does.StartWith(expectedBase));
            Assert.That(path, Does.Contain("AgentTooling"));
            Assert.That(path, Does.Contain("Smtp"));
            Assert.That(path, Does.EndWith("nebula.authrecord.json"));
        }
    }

    [Test]
    public void GetAuthRecordPath_DifferentServerNames_ProduceDifferentPaths()
    {
        var server1 = new ServerConfig { Name = "server-a", Type = "Office365", Username = "u", ClientId = "c" };
        var server2 = new ServerConfig { Name = "server-b", Type = "Office365", Username = "u", ClientId = "c" };

        var path1 = GraphClientFactory.GetAuthRecordPath(server1);
        var path2 = GraphClientFactory.GetAuthRecordPath(server2);

        Assert.That(path1, Is.Not.EqualTo(path2));
    }
}
