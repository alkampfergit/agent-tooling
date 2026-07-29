namespace Smtp.Models;

using System.Text.Json.Serialization;

public class EmailSummary
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("from")]
    public required string From { get; set; }

    [JsonPropertyName("subject")]
    public required string Subject { get; set; }

    [JsonPropertyName("date")]
    public required DateTime Date { get; set; }

    [JsonPropertyName("preview")]
    public required string Preview { get; set; }
}
