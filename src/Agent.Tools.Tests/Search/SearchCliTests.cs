namespace Agent.Tools.Tests.Search;

/// <summary>
/// Black-box tests over the real System.CommandLine parsing. Every case here fails
/// before the HTTP stage, so none of them reaches the network.
/// </summary>
[TestFixture]
[Category("search")]
[Category("Unit")]
public class SearchCliTests
{
    private const string ToolDll = "Search.dll";

    [Test]
    public void Help_ExitsZeroAndDescribesTheTool()
    {
        var (exitCode, stdOut, _) = CliRunner.Run(ToolDll, "--help");

        Assert.That(exitCode, Is.Zero);
        Assert.Multiple(() =>
        {
            Assert.That(stdOut, Does.Contain("Web search CLI tool"));
            Assert.That(stdOut, Does.Contain("query"));
            Assert.That(stdOut, Does.Contain("--engine"));
            Assert.That(stdOut, Does.Contain("--max-results"));
            Assert.That(stdOut, Does.Contain("--timeout"));
        });
    }

    [Test]
    public void Help_DescribesEveryOption()
    {
        var (_, stdOut, _) = CliRunner.Run(ToolDll, "--help");

        Assert.Multiple(() =>
        {
            Assert.That(stdOut, Does.Contain("Search terms"));
            Assert.That(stdOut, Does.Contain("Search engine to use"));
            Assert.That(stdOut, Does.Contain("Maximum number of results"));
            Assert.That(stdOut, Does.Contain("HTTP timeout"));
        });
    }

    [TestCase("")]
    [TestCase("   ")]
    public void EmptyQuery_ExitsOne(string query)
    {
        var (exitCode, _, _) = CliRunner.Run(ToolDll, query);

        Assert.That(exitCode, Is.EqualTo(1));
    }

    [Test]
    public void NoArguments_ExitsNonZero()
    {
        var (exitCode, _, _) = CliRunner.Run(ToolDll);

        Assert.That(exitCode, Is.Not.Zero);
    }

    [TestCase("0")]
    [TestCase("-1")]
    [TestCase("51")]
    public void MaxResultsOutOfRange_ExitsOne(string value)
    {
        var (exitCode, _, stdErr) = CliRunner.Run(ToolDll, "netmq", "--max-results", value);

        Assert.That(exitCode, Is.EqualTo(1));
        Assert.That(stdErr, Does.Contain("--max-results"));
    }

    [TestCase("0")]
    [TestCase("301")]
    public void TimeoutOutOfRange_ExitsOne(string value)
    {
        var (exitCode, _, stdErr) = CliRunner.Run(ToolDll, "netmq", "--timeout", value);

        Assert.That(exitCode, Is.EqualTo(1));
        Assert.That(stdErr, Does.Contain("--timeout"));
    }

    [Test]
    public void NonNumericMaxResults_ExitsOne()
    {
        var (exitCode, _, _) = CliRunner.Run(ToolDll, "netmq", "--max-results", "many");

        Assert.That(exitCode, Is.EqualTo(1));
    }

    [Test]
    public void UnconfiguredEngine_ExitsOneAndListsConfiguredEngines()
    {
        var (exitCode, stdOut, stdErr) = CliRunner.Run(ToolDll, "netmq", "--engine", "other-engine");

        Assert.That(exitCode, Is.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(stdErr, Does.Contain("not configured"));
            Assert.That(stdErr, Does.Contain("duckduckgo"));
            Assert.That(stdOut, Is.Empty, "stdout must carry JSON only");
        });
    }
}
