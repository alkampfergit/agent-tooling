namespace Search.Services;

using System.Text.Json;
using Search.Models;

/// <summary>
/// Reads the query-level abstract out of an Instant Answer API payload.
/// Pure and total: never throws, because a missing abstract must not fail a search.
/// </summary>
public static class DuckDuckGoInstantAnswerParser
{
    public static SearchAbstract? Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            // AbstractText is the plain-text form; Abstract may still carry markup when
            // the caller did not pass no_html=1.
            var text = ReadString(root, "AbstractText") ?? ReadString(root, "Abstract");
            if (text == null)
            {
                return null;
            }

            return new SearchAbstract
            {
                Text = text,
                Source = ReadString(root, "AbstractSource"),
                Url = ReadString(root, "AbstractURL")
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Returns null for a missing, non-string, empty or whitespace-only property.</summary>
    private static string? ReadString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var value = property.GetString();
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
