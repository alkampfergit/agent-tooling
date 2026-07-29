namespace Smtp.Models;

using System.Text.Json.Serialization;

public class MarkReadResult
{
    [JsonPropertyName("success")]
    public required bool Success { get; set; }

    [JsonPropertyName("id")]
    public required string Id { get; set; }
}
