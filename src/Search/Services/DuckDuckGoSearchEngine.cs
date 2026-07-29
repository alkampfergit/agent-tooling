namespace Search.Services;

using System.Net;
using Search.Models;

/// <summary>
/// Searches DuckDuckGo. Requires no API key.
///
/// Two calls per search: the official Instant Answer API for the query-level abstract,
/// and the unofficial HTML endpoint for the ranked links. Parsing lives in
/// <see cref="DuckDuckGoHtmlParser"/> and <see cref="DuckDuckGoInstantAnswerParser"/>;
/// this class does only I/O and orchestration.
/// </summary>
public class DuckDuckGoSearchEngine : ISearchEngine
{
    public const string EngineName = "duckduckgo";

    private const string InstantAnswerUrl = "https://api.duckduckgo.com/"; // NOSONAR: fixed DuckDuckGo API endpoint, not environment-dependent
    private const string HtmlResultsUrl = "https://html.duckduckgo.com/html/"; // NOSONAR: fixed DuckDuckGo API endpoint, not environment-dependent

    private readonly HttpClient _httpClient;

    public DuckDuckGoSearchEngine(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<SearchResponse> SearchAsync(
        string query, int maxResults, CancellationToken cancellationToken)
    {
        var abstractResult = await GetAbstractAsync(query, cancellationToken);
        var html = await GetResultsHtmlAsync(query, cancellationToken);

        return new SearchResponse
        {
            Query = query,
            Engine = EngineName,
            Abstract = abstractResult,
            Results = DuckDuckGoHtmlParser.Parse(html, maxResults)
        };
    }

    /// <summary>
    /// The abstract is a bonus, not a requirement: any failure here leaves it null and
    /// lets the search succeed on the links alone.
    /// </summary>
    private async Task<SearchAbstract?> GetAbstractAsync(string query, CancellationToken cancellationToken)
    {
        try
        {
            var url = $"{InstantAnswerUrl}?q={Uri.EscapeDataString(query)}&format=json&no_html=1&no_redirect=1";
            using var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return DuckDuckGoInstantAnswerParser.Parse(body);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private async Task<string> GetResultsHtmlAsync(string query, CancellationToken cancellationToken)
    {
        using var content = new FormUrlEncodedContent([new KeyValuePair<string, string>("q", query)]);
        using var response = await _httpClient.PostAsync(HtmlResultsUrl, content, cancellationToken);

        if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.TooManyRequests)
        {
            throw new SearchRuntimeException(
                $"DuckDuckGo rejected the request ({(int)response.StatusCode} {response.StatusCode}). " +
                "The HTML endpoint is unofficial and rate limited; wait before retrying.");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new SearchRuntimeException(
                $"DuckDuckGo returned HTTP {(int)response.StatusCode} ({response.StatusCode}).");
        }

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}
