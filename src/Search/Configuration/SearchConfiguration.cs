namespace Search.Configuration;

/// <summary>
/// Binding target for the "Search" configuration section.
/// </summary>
public class SearchConfiguration
{
    public List<EngineConfig> Engines { get; set; } = [];
}
