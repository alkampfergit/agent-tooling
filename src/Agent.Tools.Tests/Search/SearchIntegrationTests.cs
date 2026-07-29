namespace Agent.Tools.Tests.Search;

using global::Search;
using global::Search.Services;

/// <summary>
/// Hits the live DuckDuckGo endpoints. These are the only tests that prove the captured
/// fixtures still resemble reality. They ignore rather than fail when DuckDuckGo rate
/// limits the run, so a throttled machine does not red the suite.
/// </summary>
[TestFixture]
[Category("search")]
[Category("Integration")]
public class SearchIntegrationTests
{
    private const string UserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
        "(KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36";

    private HttpClient _httpClient = null!;

    [SetUp]
    public void SetUp()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
    }

    [TearDown]
    public void TearDown() => _httpClient.Dispose();

    [Test]
    public async Task LiveSearch_ReturnsResultsWithAbsoluteUrls()
    {
        var response = await SearchOrIgnore("netmq performance", 5);

        Assert.That(response.Results, Is.Not.Empty);
        Assert.Multiple(() =>
        {
            foreach (var result in response.Results)
            {
                Assert.That(result.Title, Is.Not.Empty);
                Assert.That(
                    Uri.IsWellFormedUriString(result.Url, UriKind.Absolute),
                    Is.True,
                    $"Not an absolute URL: {result.Url}");
                Assert.That(result.Url, Does.Not.Contain("uddg="));
            }
        });
    }

    [Test]
    public async Task LiveSearch_RespectsMaxResults()
    {
        var response = await SearchOrIgnore("zeromq", 3);

        Assert.That(response.Results, Has.Count.LessThanOrEqualTo(3));
    }

    [Test]
    public async Task LiveSearch_EncyclopedicQuery_PopulatesAbstract()
    {
        var response = await SearchOrIgnore("zeromq", 5);

        Assert.That(response.Abstract, Is.Not.Null);
        Assert.That(response.Abstract!.Text, Is.Not.Empty);
    }

    private async Task<global::Search.Models.SearchResponse> SearchOrIgnore(string query, int maxResults)
    {
        var engine = new DuckDuckGoSearchEngine(_httpClient);

        try
        {
            return await engine.SearchAsync(query, maxResults, CancellationToken.None);
        }
        catch (SearchRuntimeException ex)
        {
            Assert.Ignore($"DuckDuckGo is throttling or unreachable: {ex.Message}");
            throw;
        }
        catch (HttpRequestException ex)
        {
            Assert.Ignore($"DuckDuckGo is unreachable: {ex.Message}");
            throw;
        }
    }
}
