namespace Smtp.Models;

using System.Text.Json.Serialization;

public class EmailDetails
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("from")]
    public required string From { get; set; }

    [JsonPropertyName("to")]
    public List<string> To { get; set; } = new();

    [JsonPropertyName("cc")]
    public List<string> Cc { get; set; } = new();

    [JsonPropertyName("subject")]
    public required string Subject { get; set; }

    [JsonPropertyName("date")]
    public required DateTime Date { get; set; }

    [JsonPropertyName("body")]
    public required string Body { get; set; }
}
