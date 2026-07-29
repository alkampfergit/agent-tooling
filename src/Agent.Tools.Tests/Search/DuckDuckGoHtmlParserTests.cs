namespace Agent.Tools.Tests.Search;

using global::Search;
using global::Search.Services;

[TestFixture]
[Category("search")]
[Category("Unit")]
public class DuckDuckGoHtmlParserTests
{
    [Test]
    public void Parse_ExtractsTitlesUrlsAndSnippets()
    {
        var results = DuckDuckGoHtmlParser.Parse(SearchFixtures.Load(SearchFixtures.Results), 10);

        Assert.That(results, Is.Not.Empty);
        Assert.Multiple(() =>
        {
            foreach (var result in results)
            {
                Assert.That(result.Title, Is.Not.Empty);
                Assert.That(result.Snippet, Is.Not.Empty);
                Assert.That(
                    Uri.IsWellFormedUriString(result.Url, UriKind.Absolute),
                    Is.True,
                    $"Not an absolute URL: {result.Url}");
            }
        });
    }

    [Test]
    public void Parse_RanksResultsFromOneWithoutGaps()
    {
        var results = DuckDuckGoHtmlParser.Parse(SearchFixtures.Load(SearchFixtures.Results), 10);

        Assert.That(results.Select(r => r.Rank), Is.EqualTo(Enumerable.Range(1, results.Count)));
    }

    [Test]
    public void Parse_RespectsMaxResults()
    {
        var results = DuckDuckGoHtmlParser.Parse(SearchFixtures.Load(SearchFixtures.Results), 3);

        Assert.That(results, Has.Count.EqualTo(3));
        Assert.That(results.Select(r => r.Rank), Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void Parse_DecodesEntitiesAndCollapsesWhitespace()
    {
        var results = DuckDuckGoHtmlParser.Parse(SearchFixtures.Load(SearchFixtures.RedirectLinks), 10);

        Assert.Multiple(() =>
        {
            Assert.That(results[0].Title, Is.EqualTo("NetMQ Documentation & Guide"));
            Assert.That(results[1].Snippet, Is.EqualTo("Example snippet with collapsed whitespace."));
            foreach (var result in results)
            {
                Assert.That(result.Title, Does.Not.Contain("&amp;"));
                Assert.That(result.Snippet, Does.Not.Contain("&amp;"));
            }
        });
    }

    [Test]
    public void Parse_UnwrapsRedirectLinks()
    {
        var results = DuckDuckGoHtmlParser.Parse(SearchFixtures.Load(SearchFixtures.RedirectLinks), 10);

        Assert.Multiple(() =>
        {
            Assert.That(results[0].Url, Is.EqualTo("https://netmq.readthedocs.io/en/latest/"));
            Assert.That(results[1].Url, Is.EqualTo("https://example.com/docs?a=1&b=2"));
            Assert.That(results[2].Url, Is.EqualTo("https://github.com/zeromq/netmq"));
            foreach (var result in results)
            {
                Assert.That(result.Url, Does.Not.Contain("uddg="));
            }
        });
    }

    [TestCase(
        "//duckduckgo.com/l/?uddg=https%3A%2F%2Fexample.com%2Fa%3Fb%3Dc&rut=abc123",
        "https://example.com/a?b=c")]
    [TestCase(
        "/l/?uddg=https%3A%2F%2Fexample.com%2F",
        "https://example.com/")]
    [TestCase(
        "https://duckduckgo.com/l/?uddg=https%3A%2F%2Fexample.com%2Fpath",
        "https://example.com/path")]
    [TestCase("https://example.com/plain", "https://example.com/plain")]
    [TestCase("https://example.com/search?uddg=notaredirect", "https://example.com/search?uddg=notaredirect")]
    [TestCase("", "")]
    public void UnwrapRedirect_ReturnsTargetUrl(string href, string expected)
    {
        Assert.That(DuckDuckGoHtmlParser.UnwrapRedirect(href), Is.EqualTo(expected));
    }

    [Test]
    public void Parse_NoResultsPage_ReturnsEmptyList()
    {
        var results = DuckDuckGoHtmlParser.Parse(SearchFixtures.Load(SearchFixtures.NoResults), 10);

        Assert.That(results, Is.Empty);
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Parse_EmptyHtml_ReturnsEmptyList(string html)
    {
        Assert.That(DuckDuckGoHtmlParser.Parse(html, 10), Is.Empty);
    }

    [Test]
    public void Parse_BotChallenge_ThrowsRuntimeExceptionMentioningRateLimiting()
    {
        var html = SearchFixtures.Load(SearchFixtures.Anomaly);

        var ex = Assert.Throws<SearchRuntimeException>(() => DuckDuckGoHtmlParser.Parse(html, 10));

        Assert.That(ex!.Message, Does.Contain("rate limited"));
    }

    [Test]
    public void Parse_UnrecognisedMarkup_ThrowsRuntimeException()
    {
        var html = SearchFixtures.Load(SearchFixtures.Unparseable);

        Assert.Throws<SearchRuntimeException>(() => DuckDuckGoHtmlParser.Parse(html, 10));
    }
}
