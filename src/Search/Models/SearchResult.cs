namespace Search.Models;

using System.Text.Json.Serialization;

/// <summary>
/// A single web result returned by a search engine.
/// </summary>
public class SearchResult
{
    [JsonPropertyName("rank")]
    public required int Rank { get; set; }

    [JsonPropertyName("title")]
    public required string Title { get; set; }

    [JsonPropertyName("url")]
    public required string Url { get; set; }

    [JsonPropertyName("snippet")]
    public required string Snippet { get; set; }
}
