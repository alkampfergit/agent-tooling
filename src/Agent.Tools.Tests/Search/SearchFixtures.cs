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

    private const string ResourcePrefix = "Agent.Tools.Tests.Search.Fixtures.";

    /// <summary>
    /// Fixtures are embedded in the test assembly rather than copied to the output
    /// directory, so they resolve identically on every build agent.
    /// </summary>
    public static string Load(string fileName)
    {
        var assembly = typeof(SearchFixtures).Assembly;
        var resourceName = ResourcePrefix + fileName;

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            var available = string.Join(", ", assembly.GetManifestResourceNames());
            throw new FileNotFoundException(
                $"Embedded fixture not found: {resourceName}. Available: {available}");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
