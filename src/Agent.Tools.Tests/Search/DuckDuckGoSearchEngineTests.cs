namespace Agent.Tools.Tests.Search;

using System.Net;
using global::Search;
using global::Search.Services;

[TestFixture]
[Category("search")]
[Category("Unit")]
public class DuckDuckGoSearchEngineTests
{
    private static DuckDuckGoSearchEngine EngineOver(StubHandler handler) =>
        new(new HttpClient(handler));

    [Test]
    public async Task SearchAsync_ReturnsResultsAndAbstract()
    {
        var engine = EngineOver(new StubHandler());

        var response = await engine.SearchAsync("netmq performance", 10, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(response.Query, Is.EqualTo("netmq performance"));
            Assert.That(response.Engine, Is.EqualTo("duckduckgo"));
            Assert.That(response.Results, Is.Not.Empty);
            Assert.That(response.Abstract, Is.Not.Null);
            Assert.That(response.Abstract!.Text, Does.Contain("ZeroMQ"));
        });
    }

    [Test]
    public async Task SearchAsync_TruncatesToMaxResults()
    {
        var engine = EngineOver(new StubHandler());

        var response = await engine.SearchAsync("netmq", 2, CancellationToken.None);

        Assert.That(response.Results, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task SearchAsync_InstantAnswerFails_AbstractIsNullAndSearchSucceeds()
    {
        var engine = EngineOver(new StubHandler { ThrowOnInstantAnswer = true });

        var response = await engine.SearchAsync("netmq", 10, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(response.Abstract, Is.Null);
            Assert.That(response.Results, Is.Not.Empty);
        });
    }

    [Test]
    public async Task SearchAsync_InstantAnswerReturnsError_AbstractIsNullAndSearchSucceeds()
    {
        var engine = EngineOver(new StubHandler { InstantAnswerStatus = HttpStatusCode.ServiceUnavailable });

        var response = await engine.SearchAsync("netmq", 10, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(response.Abstract, Is.Null);
            Assert.That(response.Results, Is.Not.Empty);
        });
    }

    [Test]
    public async Task SearchAsync_NoResultsPage_ReturnsEmptyResults()
    {
        var engine = EngineOver(new StubHandler { HtmlFixture = SearchFixtures.NoResults });

        var response = await engine.SearchAsync("nothing matches", 10, CancellationToken.None);

        Assert.That(response.Results, Is.Empty);
    }

    [TestCase(HttpStatusCode.Forbidden)]
    [TestCase(HttpStatusCode.TooManyRequests)]
    public void SearchAsync_RejectedByEngine_ThrowsRuntimeExceptionMentioningRateLimiting(
        HttpStatusCode status)
    {
        var engine = EngineOver(new StubHandler { HtmlStatus = status });

        var ex = Assert.ThrowsAsync<SearchRuntimeException>(
            () => engine.SearchAsync("netmq", 10, CancellationToken.None));

        Assert.That(ex!.Message, Does.Contain("rate limited"));
    }

    [Test]
    public void SearchAsync_ServerError_ThrowsRuntimeExceptionWithStatusCode()
    {
        var engine = EngineOver(new StubHandler { HtmlStatus = HttpStatusCode.InternalServerError });

        var ex = Assert.ThrowsAsync<SearchRuntimeException>(
            () => engine.SearchAsync("netmq", 10, CancellationToken.None));

        Assert.That(ex!.Message, Does.Contain("500"));
    }

    [Test]
    public void SearchAsync_BotChallenge_ThrowsRuntimeException()
    {
        var engine = EngineOver(new StubHandler { HtmlFixture = SearchFixtures.Anomaly });

        var ex = Assert.ThrowsAsync<SearchRuntimeException>(
            () => engine.SearchAsync("netmq", 10, CancellationToken.None));

        Assert.That(ex!.Message, Does.Contain("bot-verification"));
    }

    [Test]
    public async Task SearchAsync_SendsQueryToBothEndpoints()
    {
        var handler = new StubHandler();
        var engine = EngineOver(handler);

        await engine.SearchAsync("netmq performance", 10, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(handler.InstantAnswerUri, Does.Contain("q=netmq%20performance"));
            Assert.That(handler.InstantAnswerUri, Does.Contain("format=json"));
            Assert.That(handler.HtmlRequestBody, Is.EqualTo("q=netmq+performance"));
        });
    }

    /// <summary>
    /// Serves the captured fixtures in place of DuckDuckGo, routing on the request host.
    /// </summary>
    private sealed class StubHandler : HttpMessageHandler
    {
        public string HtmlFixture { get; init; } = SearchFixtures.Results;

        public HttpStatusCode HtmlStatus { get; init; } = HttpStatusCode.OK;

        public HttpStatusCode InstantAnswerStatus { get; init; } = HttpStatusCode.OK;

        public bool ThrowOnInstantAnswer { get; init; }

        public string? InstantAnswerUri { get; private set; }

        public string? HtmlRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri!;

            if (uri.Host == "api.duckduckgo.com")
            {
                if (ThrowOnInstantAnswer)
                {
                    throw new HttpRequestException("simulated network failure");
                }

                InstantAnswerUri = uri.AbsoluteUri;

                return new HttpResponseMessage(InstantAnswerStatus)
                {
                    Content = new StringContent(
                        SearchFixtures.Load(SearchFixtures.InstantAnswerWithAbstract))
                };
            }

            HtmlRequestBody = request.Content == null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(HtmlStatus)
            {
                Content = new StringContent(SearchFixtures.Load(HtmlFixture))
            };
        }
    }
}
