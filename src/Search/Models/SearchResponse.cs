namespace Search.Models;

using System.Text.Json.Serialization;

/// <summary>
/// The complete result of one search. Property order matches the documented JSON
/// output; System.Text.Json writes properties in declaration order.
/// </summary>
public class SearchResponse
{
    [JsonPropertyName("query")]
    public required string Query { get; set; }

    [JsonPropertyName("engine")]
    public required string Engine { get; set; }

    /// <summary>Null when the engine has no instant answer for the query.</summary>
    [JsonPropertyName("abstract")]
    public SearchAbstract? Abstract { get; set; }

    [JsonPropertyName("results")]
    public IReadOnlyList<SearchResult> Results { get; set; } = [];
}
