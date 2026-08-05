namespace Smtp.Models;

using System.Text.Json.Serialization;

public class MarkReadSummary
{
    [JsonPropertyName("total")]
    public required int Total { get; set; }

    [JsonPropertyName("succeeded")]
    public required int Succeeded { get; set; }

    [JsonPropertyName("failed")]
    public required List<string> Failed { get; set; }
}
