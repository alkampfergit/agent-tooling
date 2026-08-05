namespace Smtp.Configuration;

using Microsoft.Extensions.Configuration;

public class ConfigurationProvider
{
    private readonly IConfiguration _config;

    public ConfigurationProvider()
    {
        _config = Agent.Tools.Common.ConfigurationManager.LoadToolConfiguration("Smtp");
    }

    public SmtpConfiguration GetSmtpConfiguration()
    {
        var config = new SmtpConfiguration { Servers = new() };
        _config.Bind(config);
        return config;
    }

    public ServerConfig GetServer(string? serverName)
    {
        var config = GetSmtpConfiguration();

        if (config.Servers == null || config.Servers.Count == 0)
            throw new InvalidOperationException(
                "No servers configured in 'Smtp.Servers'. " +
                "Configure in appsettings.json or agent-tooling.json");

        if (string.IsNullOrWhiteSpace(serverName))
        {
            if (config.Servers.Count == 1)
            {
                config.Servers[0].Validate();
                return config.Servers[0];
            }

            var defaultServer = config.Servers.FirstOrDefault(s => s.Default == true);
            if (defaultServer != null)
            {
                defaultServer.Validate();
                return defaultServer;
            }

            var names = string.Join(", ", config.Servers.Select(s => s.Name));
            throw new InvalidOperationException(
                $"Multiple servers configured ({names}). " +
                $"Specify one with --servername");
        }

        var server = config.Servers.FirstOrDefault(s => s.Name == serverName);
        if (server == null)
        {
            var names = string.Join(", ", config.Servers.Select(s => s.Name));
            throw new InvalidOperationException(
                $"Server '{serverName}' not found. " +
                $"Available: {names}");
        }

        server.Validate();
        return server;
    }
}
