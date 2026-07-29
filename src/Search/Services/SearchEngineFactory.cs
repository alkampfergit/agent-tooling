namespace Search.Services;

/// <summary>
/// Maps a resolved engine name to its implementation. Adding an engine means adding a
/// case here and an entry to <see cref="Supported"/>.
/// </summary>
public static class SearchEngineFactory
{
    public static readonly IReadOnlyList<string> Supported = [DuckDuckGoSearchEngine.EngineName];

    /// <exception cref="SearchConfigurationException">The name is configured but not implemented.</exception>
    public static ISearchEngine Create(string engineName, HttpClient httpClient) =>
        engineName.ToLowerInvariant() switch
        {
            DuckDuckGoSearchEngine.EngineName => new DuckDuckGoSearchEngine(httpClient),
            _ => throw new SearchConfigurationException(
                $"Engine '{engineName}' is configured but not supported by this build. " +
                $"Supported: {string.Join(", ", Supported)}")
        };
}
