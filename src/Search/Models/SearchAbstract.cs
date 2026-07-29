namespace Search.Models;

using System.Text.Json.Serialization;

/// <summary>
/// A query-level summary (a "zero-click" / instant answer), not a per-result summary.
/// At most one is returned per search, and only for queries the engine has an
/// encyclopedic entry for.
/// </summary>
public class SearchAbstract
{
    [JsonPropertyName("text")]
    public required string Text { get; set; }

    [JsonPropertyName("source")]
    public string? Source { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}
