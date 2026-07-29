namespace Agent.Tools.Tests.Search;

using Microsoft.Extensions.Configuration;
using global::Search;
using global::Search.Configuration;
using global::Search.Services;
using ConfigurationProvider = global::Search.Configuration.ConfigurationProvider;

/// <summary>
/// Covers every row of the engine-resolution table in the Search specification.
/// </summary>
[TestFixture]
[Category("search")]
[Category("Unit")]
public class EngineResolutionTests
{
    /// <summary>
    /// Builds a provider over the "Search" section, matching what
    /// ConfigurationManager.LoadToolConfiguration returns at runtime.
    /// </summary>
    private static ConfigurationProvider ProviderFor(params (string Name, bool Default)[] engines)
    {
        var values = new Dictionary<string, string?>();
        for (var i = 0; i < engines.Length; i++)
        {
            values[$"Search:Engines:{i}:Name"] = engines[i].Name;
            values[$"Search:Engines:{i}:Default"] = engines[i].Default ? "true" : "false";
        }

        var section = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build()
            .GetSection("Search");

        return new ConfigurationProvider(section);
    }

    [Test]
    public void ResolveEngine_ExplicitEngine_MatchesConfiguredName()
    {
        var provider = ProviderFor(("duckduckgo", true), ("other-engine", false));

        Assert.That(provider.ResolveEngine("other-engine"), Is.EqualTo("other-engine"));
    }

    [TestCase("DuckDuckGo")]
    [TestCase("DUCKDUCKGO")]
    public void ResolveEngine_ExplicitEngine_IsCaseInsensitive(string requested)
    {
        var provider = ProviderFor(("duckduckgo", true));

        Assert.That(provider.ResolveEngine(requested), Is.EqualTo("duckduckgo"));
    }

    [Test]
    public void ResolveEngine_UnknownEngine_ThrowsAndListsConfiguredNames()
    {
        var provider = ProviderFor(("duckduckgo", true), ("other-engine", false));

        var ex = Assert.Throws<SearchConfigurationException>(() => provider.ResolveEngine("third-engine"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex!.Message, Does.Contain("Engine 'third-engine' not configured"));
            Assert.That(ex.Message, Does.Contain("duckduckgo, other-engine"));
        }
    }

    [Test]
    public void ResolveEngine_SingleEngineWithoutDefaultFlag_UsesIt()
    {
        var provider = ProviderFor(("duckduckgo", false));

        Assert.That(provider.ResolveEngine(null), Is.EqualTo("duckduckgo"));
    }

    [Test]
    public void ResolveEngine_SingleDefaultAmongSeveral_UsesTheDefault()
    {
        var provider = ProviderFor(("other-engine", false), ("duckduckgo", true), ("third-engine", false));

        Assert.That(provider.ResolveEngine(null), Is.EqualTo("duckduckgo"));
    }

    [Test]
    public void ResolveEngine_MultipleEnginesNoDefault_ThrowsExplainingHowToDisambiguate()
    {
        var provider = ProviderFor(("duckduckgo", false), ("other-engine", false));

        var ex = Assert.Throws<SearchConfigurationException>(() => provider.ResolveEngine(null));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex!.Message, Does.Contain("Multiple engines configured"));
            Assert.That(ex.Message, Does.Contain("duckduckgo, other-engine"));
            Assert.That(ex.Message, Does.Contain("--engine"));
        }
    }

    [Test]
    public void ResolveEngine_MultipleDefaults_Throws()
    {
        var provider = ProviderFor(("duckduckgo", true), ("other-engine", true));

        var ex = Assert.Throws<SearchConfigurationException>(() => provider.ResolveEngine(null));

        Assert.That(ex!.Message, Does.Contain("Multiple default engines configured"));
    }

    [Test]
    public void ResolveEngine_MultipleDefaults_ThrowsEvenWithExplicitEngine()
    {
        // A configuration with two defaults is a mistake worth surfacing even when the
        // user is overriding it.
        var provider = ProviderFor(("duckduckgo", true), ("other-engine", true));

        var ex = Assert.Throws<SearchConfigurationException>(() => provider.ResolveEngine("other-engine"));

        Assert.That(ex!.Message, Does.Contain("Only one may set"));
    }

    [Test]
    public void ResolveEngine_NoEnginesConfigured_FallsBackToDuckDuckGo()
    {
        var provider = ProviderFor();

        Assert.That(provider.ResolveEngine(null), Is.EqualTo("duckduckgo"));
    }

    [Test]
    public void GetSearchConfiguration_NoEnginesConfigured_ReturnsSingleDefaultEngine()
    {
        var engines = ProviderFor().GetSearchConfiguration().Engines;

        Assert.That(engines, Has.Count.EqualTo(1));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(engines[0].Name, Is.EqualTo("duckduckgo"));
            Assert.That(engines[0].Default, Is.True);
        }
    }

    [Test]
    public void Factory_UnsupportedEngine_ThrowsListingSupportedEngines()
    {
        using var httpClient = new HttpClient();

        var ex = Assert.Throws<SearchConfigurationException>(
            () => SearchEngineFactory.Create("other-engine", httpClient));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex!.Message, Does.Contain("not supported by this build"));
            Assert.That(ex.Message, Does.Contain("duckduckgo"));
        }
    }

    [Test]
    public void Factory_DuckDuckGo_ReturnsEngine()
    {
        using var httpClient = new HttpClient();

        Assert.That(SearchEngineFactory.Create("duckduckgo", httpClient),
            Is.InstanceOf<DuckDuckGoSearchEngine>());
    }
}
