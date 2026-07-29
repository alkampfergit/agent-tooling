namespace Agent.Tools.Tests.Search;

using global::Search.Services;

[TestFixture]
[Category("search")]
[Category("Unit")]
public class DuckDuckGoInstantAnswerParserTests
{
    [Test]
    public void Parse_WithAbstract_ReturnsTextSourceAndUrl()
    {
        var result = DuckDuckGoInstantAnswerParser.Parse(
            SearchFixtures.Load(SearchFixtures.InstantAnswerWithAbstract));

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.Text, Does.Contain("ZeroMQ"));
            Assert.That(result.Source, Is.EqualTo("Wikipedia"));
            Assert.That(result.Url, Does.StartWith("https://"));
        });
    }

    [Test]
    public void Parse_WithoutAbstract_ReturnsNull()
    {
        var result = DuckDuckGoInstantAnswerParser.Parse(
            SearchFixtures.Load(SearchFixtures.InstantAnswerEmpty));

        Assert.That(result, Is.Null);
    }

    [Test]
    public void Parse_PrefersAbstractTextOverAbstract()
    {
        const string json = """
            {"Abstract":"<b>markup</b>","AbstractText":"plain text","AbstractSource":"Wikipedia"}
            """;

        var result = DuckDuckGoInstantAnswerParser.Parse(json);

        Assert.That(result!.Text, Is.EqualTo("plain text"));
    }

    [Test]
    public void Parse_FallsBackToAbstractWhenAbstractTextIsEmpty()
    {
        const string json = """
            {"Abstract":"only this","AbstractText":"","AbstractSource":"Wikipedia"}
            """;

        var result = DuckDuckGoInstantAnswerParser.Parse(json);

        Assert.That(result!.Text, Is.EqualTo("only this"));
    }

    [Test]
    public void Parse_EmptySourceAndUrl_AreNull()
    {
        const string json = """
            {"AbstractText":"some text","AbstractSource":"","AbstractURL":""}
            """;

        var result = DuckDuckGoInstantAnswerParser.Parse(json);

        Assert.Multiple(() =>
        {
            Assert.That(result!.Source, Is.Null);
            Assert.That(result.Url, Is.Null);
        });
    }

    [TestCase("not json at all")]
    [TestCase("{\"AbstractText\":")]
    [TestCase("[1,2,3]")]
    [TestCase("")]
    [TestCase("   ")]
    public void Parse_MalformedOrUnexpectedJson_ReturnsNullWithoutThrowing(string json)
    {
        Assert.That(DuckDuckGoInstantAnswerParser.Parse(json), Is.Null);
    }
}
