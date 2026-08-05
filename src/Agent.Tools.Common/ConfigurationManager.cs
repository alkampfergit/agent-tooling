using Microsoft.Extensions.Configuration;

namespace Agent.Tools.Common;

public static class ConfigurationManager
{
    public static IConfiguration LoadToolConfiguration(string toolName)
    {
        var configBuilder = new ConfigurationBuilder();
        var currentDir = Directory.GetCurrentDirectory();
        var searchDir = new DirectoryInfo(currentDir);

        // Search for agent-tooling.json in parent directories
        string? agentToolingPath = null;
        while (searchDir != null)
        {
            var candidatePath = Path.Combine(searchDir.FullName, "agent-tooling.json");
            if (File.Exists(candidatePath))
            {
                agentToolingPath = candidatePath;
                break;
            }
            searchDir = searchDir.Parent;
        }

        // Load agent-tooling.json if found
        if (!string.IsNullOrEmpty(agentToolingPath))
        {
            configBuilder.AddJsonFile(agentToolingPath, optional: true, reloadOnChange: false);
        }

        // Load tool-specific appsettings.json from current directory
        var appSettingsPath = Path.Combine(currentDir, "appsettings.json");
        if (File.Exists(appSettingsPath))
        {
            configBuilder.AddJsonFile(appSettingsPath, optional: true, reloadOnChange: false);
        }

        var config = configBuilder.Build();
        return config.GetSection(toolName);
    }
}
