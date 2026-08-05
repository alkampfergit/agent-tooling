namespace Agent.Tools.Tests.Smtp;

using global::Smtp.Commands;

[TestFixture]
[Category("smtp")]
[Category("Unit")]
public class MarkReadCommandTests
{
    [Test]
    public void ParseIds_WithSingleId_ReturnsOneId()
    {
        var ids = MarkReadCommand.ParseIds("42");

        Assert.That(ids, Is.EqualTo(new[] { "42" }));
    }

    [Test]
    public void ParseIds_WithCommaSeparatedList_TrimsAndFiltersEmptyEntries()
    {
        var ids = MarkReadCommand.ParseIds(" 1, 2 ,,3 ");

        Assert.That(ids, Is.EqualTo(new[] { "1", "2", "3" }));
    }

    [Test]
    public void BuildSummary_WithAllSuccesses_ReturnsExitCodeZero()
    {
        var results = new List<(string Id, bool Success)>
        {
            ("1", true),
            ("2", true)
        };

        var (summary, exitCode) = MarkReadCommand.BuildSummary(results);

        Assert.Multiple(() =>
        {
            Assert.That(summary.Total, Is.EqualTo(2));
            Assert.That(summary.Succeeded, Is.EqualTo(2));
            Assert.That(summary.Failed, Is.Empty);
            Assert.That(exitCode, Is.EqualTo(0));
        });
    }

    [Test]
    public void BuildSummary_WithSomeFailures_ReturnsFailedIdsAndExitCodeTwo()
    {
        var results = new List<(string Id, bool Success)>
        {
            ("1", true),
            ("2", false),
            ("3", false)
        };

        var (summary, exitCode) = MarkReadCommand.BuildSummary(results);

        Assert.Multiple(() =>
        {
            Assert.That(summary.Total, Is.EqualTo(3));
            Assert.That(summary.Succeeded, Is.EqualTo(1));
            Assert.That(summary.Failed, Is.EqualTo(new[] { "2", "3" }));
            Assert.That(exitCode, Is.EqualTo(2));
        });
    }
}
