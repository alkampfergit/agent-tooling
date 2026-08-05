namespace Agent.Tools.Tests.Smtp;

using System.Text.Json;
using global::Smtp.Configuration;

[TestFixture]
[Category("smtp")]
[Category("Unit")]
public class ConfigurationProviderTests
{
    [Test]
    public void GetSmtpConfiguration_BindsCorrectly()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var originalDir = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(tempDir);

            var config = new
            {
                Smtp = new
                {
                    Servers = new[]
                    {
                        new { Name = "test", Address = "imap.test.com", Port = 993, Username = "user", Password = "pass", UseHttps = true }
                    }
                }
            };

            var json = JsonSerializer.Serialize(config);
            File.WriteAllText(Path.Combine(tempDir, "appsettings.json"), json);

            var provider = new ConfigurationProvider();
            var smtpConfig = provider.GetSmtpConfiguration();

            Assert.That(smtpConfig.Servers, Is.Not.Null);
            Assert.That(smtpConfig.Servers.Count, Is.EqualTo(1));
            Assert.That(smtpConfig.Servers[0].Name, Is.EqualTo("test"));

            Directory.SetCurrentDirectory(originalDir);
        }
        finally
        {
            Directory.SetCurrentDirectory(Directory.GetCurrentDirectory());
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public void GetServer_WithSingleServer_ReturnsIt()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var originalDir = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(tempDir);

            var config = new
            {
                Smtp = new
                {
                    Servers = new[]
                    {
                        new { Name = "test", Address = "imap.test.com", Port = 993, Username = "user", Password = "pass", UseHttps = true }
                    }
                }
            };

            var json = JsonSerializer.Serialize(config);
            File.WriteAllText(Path.Combine(tempDir, "appsettings.json"), json);

            var provider = new ConfigurationProvider();
            var server = provider.GetServer(null);

            Assert.That(server, Is.Not.Null);
            Assert.That(server.Name, Is.EqualTo("test"));
            Assert.That(server.Address, Is.EqualTo("imap.test.com"));

            Directory.SetCurrentDirectory(originalDir);
        }
        finally
        {
            Directory.SetCurrentDirectory(Directory.GetCurrentDirectory());
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public void GetServer_WithMultipleServers_ThrowsWithoutName()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var originalDir = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(tempDir);

            var config = new
            {
                Smtp = new
                {
                    Servers = new[]
                    {
                        new { Name = "primary", Address = "imap1.test.com", Port = 993, Username = "user1", Password = "pass1", UseHttps = true },
                        new { Name = "secondary", Address = "imap2.test.com", Port = 993, Username = "user2", Password = "pass2", UseHttps = true }
                    }
                }
            };

            var json = JsonSerializer.Serialize(config);
            File.WriteAllText(Path.Combine(tempDir, "appsettings.json"), json);

            var provider = new ConfigurationProvider();
            var ex = Assert.Throws<InvalidOperationException>(() => provider.GetServer(null));
            Assert.That(ex!.Message, Does.Contain("Multiple servers configured"));

            Directory.SetCurrentDirectory(originalDir);
        }
        finally
        {
            Directory.SetCurrentDirectory(Directory.GetCurrentDirectory());
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public void GetServer_WithValidName_ReturnsIt()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var originalDir = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(tempDir);

            var config = new
            {
                Smtp = new
                {
                    Servers = new[]
                    {
                        new { Name = "primary", Address = "imap1.test.com", Port = 993, Username = "user1", Password = "pass1", UseHttps = true },
                        new { Name = "secondary", Address = "imap2.test.com", Port = 993, Username = "user2", Password = "pass2", UseHttps = true }
                    }
                }
            };

            var json = JsonSerializer.Serialize(config);
            File.WriteAllText(Path.Combine(tempDir, "appsettings.json"), json);

            var provider = new ConfigurationProvider();
            var server = provider.GetServer("secondary");

            Assert.That(server, Is.Not.Null);
            Assert.That(server.Name, Is.EqualTo("secondary"));
            Assert.That(server.Address, Is.EqualTo("imap2.test.com"));

            Directory.SetCurrentDirectory(originalDir);
        }
        finally
        {
            Directory.SetCurrentDirectory(Directory.GetCurrentDirectory());
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public void GetServer_WithInvalidName_ThrowsNotFound()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var originalDir = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(tempDir);

            var config = new
            {
                Smtp = new
                {
                    Servers = new[]
                    {
                        new { Name = "primary", Address = "imap.test.com", Port = 993, Username = "user", Password = "pass", UseHttps = true }
                    }
                }
            };

            var json = JsonSerializer.Serialize(config);
            File.WriteAllText(Path.Combine(tempDir, "appsettings.json"), json);

            var provider = new ConfigurationProvider();
            var ex = Assert.Throws<InvalidOperationException>(() => provider.GetServer("nonexistent"));
            Assert.That(ex!.Message, Does.Contain("not found"));

            Directory.SetCurrentDirectory(originalDir);
        }
        finally
        {
            Directory.SetCurrentDirectory(Directory.GetCurrentDirectory());
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public void GetServer_WithOffice365Type_BindsAndValidates()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var originalDir = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(tempDir);

            var config = new
            {
                Smtp = new
                {
                    Servers = new[]
                    {
                        new { Name = "o365", Type = "Office365", Username = "user@contoso.com", ClientId = "client-id", TenantId = "common" }
                    }
                }
            };

            var json = JsonSerializer.Serialize(config);
            File.WriteAllText(Path.Combine(tempDir, "appsettings.json"), json);

            var provider = new ConfigurationProvider();
            var server = provider.GetServer(null);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(server.Type, Is.EqualTo("Office365"));
                Assert.That(server.Username, Is.EqualTo("user@contoso.com"));
                Assert.That(server.ClientId, Is.EqualTo("client-id"));
            }

            Directory.SetCurrentDirectory(originalDir);
        }
        finally
        {
            Directory.SetCurrentDirectory(Directory.GetCurrentDirectory());
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public void GetServer_WithOffice365MissingClientId_ThrowsFromValidate()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var originalDir = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(tempDir);

            var config = new
            {
                Smtp = new
                {
                    Servers = new[]
                    {
                        new { Name = "o365", Type = "Office365", Username = "user@contoso.com" }
                    }
                }
            };

            var json = JsonSerializer.Serialize(config);
            File.WriteAllText(Path.Combine(tempDir, "appsettings.json"), json);

            var provider = new ConfigurationProvider();
            var ex = Assert.Throws<InvalidOperationException>(() => provider.GetServer(null));
            Assert.That(ex!.Message, Does.Contain("ClientId"));

            Directory.SetCurrentDirectory(originalDir);
        }
        finally
        {
            Directory.SetCurrentDirectory(Directory.GetCurrentDirectory());
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public void GetServer_WithNoServers_ThrowsInvalidOperation()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var originalDir = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(tempDir);

            var config = new { Smtp = new { Servers = new object[0] } };
            var json = JsonSerializer.Serialize(config);
            File.WriteAllText(Path.Combine(tempDir, "appsettings.json"), json);

            var provider = new ConfigurationProvider();
            var ex = Assert.Throws<InvalidOperationException>(() => provider.GetServer(null));
            Assert.That(ex!.Message, Does.Contain("No servers configured"));

            Directory.SetCurrentDirectory(originalDir);
        }
        finally
        {
            Directory.SetCurrentDirectory(Directory.GetCurrentDirectory());
            Directory.Delete(tempDir, true);
        }
    }
}
