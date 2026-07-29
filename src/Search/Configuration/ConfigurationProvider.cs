namespace Search.Configuration;

using Microsoft.Extensions.Configuration;

/// <summary>
/// Loads the "Search" configuration section and resolves which engine a run should use.
/// </summary>
public class ConfigurationProvider
{
    /// <summary>
    /// Used when no configuration file is present, or the Engines list is empty, so the
    /// tool works with zero configuration.
    /// </summary>
    public const string FallbackEngineName = "duckduckgo";

    private readonly IConfiguration _config;

    public ConfigurationProvider()
        : this(Agent.Tools.Common.ConfigurationManager.LoadToolConfiguration("Search"))
    {
    }

    /// <summary>
    /// Takes the already-resolved "Search" section. Lets callers supply configuration
    /// directly instead of going through the file system.
    /// </summary>
    public ConfigurationProvider(IConfiguration config)
    {
        _config = config;
    }

    public SearchConfiguration GetSearchConfiguration()
    {
        var config = new SearchConfiguration();
        _config.Bind(config);

        if (config.Engines.Count == 0)
        {
            config.Engines = [new EngineConfig { Name = FallbackEngineName, Default = true }];
        }

        return config;
    }

    /// <summary>
    /// Returns the name of the engine to use, lowercased.
    /// </summary>
    /// <param name="requested">The value of --engine, or null when it was omitted.</param>
    public string ResolveEngine(string? requested)
    {
        var engines = GetSearchConfiguration().Engines;

        // A configuration with more than one default is a mistake worth surfacing even
        // when the user is overriding it with --engine.
        var defaults = engines.Where(e => e.Default).ToList();
        if (defaults.Count > 1)
        {
            throw new SearchConfigurationException(
                $"Multiple default engines configured ({Names(defaults)}). " +
                "Only one may set \"Default\": true");
        }

        if (!string.IsNullOrWhiteSpace(requested))
        {
            var match = engines.FirstOrDefault(
                e => string.Equals(e.Name, requested, StringComparison.OrdinalIgnoreCase));

            if (match == null)
            {
                throw new SearchConfigurationException(
                    $"Engine '{requested}' not configured. Configured: {Names(engines)}");
            }

            return match.Name.ToLowerInvariant();
        }

        if (defaults.Count == 1)
        {
            return defaults[0].Name.ToLowerInvariant();
        }

        if (engines.Count == 1)
        {
            return engines[0].Name.ToLowerInvariant();
        }

        throw new SearchConfigurationException(
            $"Multiple engines configured ({Names(engines)}). " +
            "Set \"Default\": true on one or pass --engine");
    }

    private static string Names(IEnumerable<EngineConfig> engines) =>
        string.Join(", ", engines.Select(e => e.Name));
}
