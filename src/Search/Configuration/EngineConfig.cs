namespace Search.Configuration;

/// <summary>
/// One configured search engine. Engines that require credentials will add their
/// own properties when they are implemented.
/// </summary>
public class EngineConfig
{
    /// <summary>Engine identifier, matched case-insensitively against implemented engines.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>When true, this engine is used if --engine is omitted.</summary>
    public bool Default { get; set; }
}
