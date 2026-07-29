namespace Search.Services;

using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Search.Models;

/// <summary>
/// Turns a response body from https://html.duckduckgo.com/html/ into search results.
/// Pure: no network access, so the parsing rules can be exercised against captured pages.
/// </summary>
public static class DuckDuckGoHtmlParser
{
    private const string ResultAnchorXPath = "//a[contains(@class,'result__a')]";
    private const string SnippetXPath = ".//*[contains(@class,'result__snippet')]";
    private const string ResultBlockXPath = "ancestor::div[contains(@class,'result')][1]";
    private const string ResultsContainerXPath = "//*[@id='links']";
    private const string AnomalyXPath = "//*[contains(@class,'anomaly-modal')]";

    private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);

    /// <summary>
    /// Extracts up to <paramref name="maxResults"/> results, ranked from 1.
    /// </summary>
    /// <exception cref="SearchRuntimeException">
    /// The body is a bot challenge, or is not recognisable as a results page at all.
    /// </exception>
    public static IReadOnlyList<SearchResult> Parse(string html, int maxResults)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return [];
        }

        var document = new HtmlDocument();
        document.LoadHtml(html);
        var root = document.DocumentNode;

        var anchors = root.SelectNodes(ResultAnchorXPath);

        if (anchors == null || anchors.Count == 0)
        {
            // DuckDuckGo serves its bot challenge with HTTP 200, so an empty result set
            // and "you have been rate limited" look identical at the status-code level.
            if (root.SelectSingleNode(AnomalyXPath) != null)
            {
                throw new SearchRuntimeException(
                    "DuckDuckGo returned a bot-verification challenge instead of results. " +
                    "The HTML endpoint is unofficial and rate limited; wait before retrying.");
            }

            // The results container present but empty is a genuine zero-result search.
            if (root.SelectSingleNode(ResultsContainerXPath) != null)
            {
                return [];
            }

            throw new SearchRuntimeException(
                "Could not parse the DuckDuckGo response: no results and no recognised " +
                "results container. The HTML endpoint is unofficial and its markup may have changed.");
        }

        var results = new List<SearchResult>();

        foreach (var anchor in anchors)
        {
            if (results.Count == maxResults)
            {
                break;
            }

            var title = Clean(anchor.InnerText);
            var url = UnwrapRedirect(anchor.GetAttributeValue("href", string.Empty));

            if (title.Length == 0 || url.Length == 0)
            {
                continue;
            }

            var block = anchor.SelectSingleNode(ResultBlockXPath);
            var snippetNode = block?.SelectSingleNode(SnippetXPath);

            results.Add(new SearchResult
            {
                Rank = results.Count + 1,
                Title = title,
                Url = url,
                Snippet = snippetNode == null ? string.Empty : Clean(snippetNode.InnerText)
            });
        }

        return results;
    }

    /// <summary>
    /// Turns a DuckDuckGo redirect link (<c>/l/?uddg=&lt;encoded&gt;</c>) into the target URL.
    /// Anything that is not a redirect is returned unchanged.
    /// </summary>
    public static string UnwrapRedirect(string href)
    {
        if (string.IsNullOrWhiteSpace(href))
        {
            return string.Empty;
        }

        href = HtmlEntity.DeEntitize(href).Trim();

        var queryStart = href.IndexOf('?');
        if (queryStart < 0 || !href[..queryStart].EndsWith("/l/", StringComparison.Ordinal))
        {
            return href;
        }

        foreach (var pair in href[(queryStart + 1)..].Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = pair.IndexOf('=');
            if (separator > 0 && pair[..separator] == "uddg")
            {
                var target = Uri.UnescapeDataString(pair[(separator + 1)..]);
                if (target.Length > 0)
                {
                    return target;
                }
            }
        }

        return href;
    }

    private static string Clean(string value) =>
        Whitespace.Replace(HtmlEntity.DeEntitize(value) ?? string.Empty, " ").Trim();
}
