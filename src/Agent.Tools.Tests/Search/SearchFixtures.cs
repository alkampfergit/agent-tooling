namespace Agent.Tools.Tests.Search;

/// <summary>
/// Loads the captured DuckDuckGo responses that the parser tests run against.
/// </summary>
internal static class SearchFixtures
{
    /// <summary>A real results page for "netmq performance" (10 results, direct URLs).</summary>
    public const string Results = "duckduckgo-results.html";

    /// <summary>The same page with the result blocks removed: results container, no results.</summary>
    public const string NoResults = "duckduckgo-no-results.html";

    /// <summary>A real bot-verification challenge, which DuckDuckGo serves with HTTP 200.</summary>
    public const string Anomaly = "duckduckgo-anomaly.html";

    /// <summary>Result markup using the /l/?uddg= redirect href form.</summary>
    public const string RedirectLinks = "duckduckgo-redirect-links.html";

    /// <summary>A non-empty page that is not a DuckDuckGo results page at all.</summary>
    public const string Unparseable = "duckduckgo-unparseable.html";

    public const string InstantAnswerWithAbstract = "instant-answer-with-abstract.json";

    public const string InstantAnswerEmpty = "instant-answer-empty.json";

    public static string Load(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Search", "Fixtures", fileName);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Fixture not found: {path}");
        }

        return File.ReadAllText(path);
    }
}
