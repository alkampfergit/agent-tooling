namespace Agent.Tools.Tests.Smtp;

using System.Text.Json;
using global::Smtp.Commands;

[TestFixture]
[Category("smtp")]
[Category("Unit")]
public class MarkReadCommandTests
{
    private static readonly string[] SingleId = { "42" };
    private static readonly string[] ThreeIds = { "1", "2", "3" };
    private static readonly string[] FailedIds = { "2", "3" };
    private static readonly string[] IdArgOne = { "--id", "1" };

    [Test]
    public void ParseIds_WithSingleId_ReturnsOneId()
    {
        var ids = MarkReadCommand.ParseIds("42");

        Assert.That(ids, Is.EqualTo(SingleId));
    }

    [Test]
    public void ParseIds_WithCommaSeparatedList_TrimsAndFiltersEmptyEntries()
    {
        var ids = MarkReadCommand.ParseIds(" 1, 2 ,,3 ");

        Assert.That(ids, Is.EqualTo(ThreeIds));
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

        using (Assert.EnterMultipleScope())
        {
            Assert.That(summary.Total, Is.EqualTo(2));
            Assert.That(summary.Succeeded, Is.EqualTo(2));
            Assert.That(summary.Failed, Is.Empty);
            Assert.That(exitCode, Is.Zero);
        }
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

        using (Assert.EnterMultipleScope())
        {
            Assert.That(summary.Total, Is.EqualTo(3));
            Assert.That(summary.Succeeded, Is.EqualTo(1));
            Assert.That(summary.Failed, Is.EqualTo(FailedIds));
            Assert.That(exitCode, Is.EqualTo(2));
        }
    }

    [Test]
    public void ExecuteCore_WithMissingId_ReturnsExitCodeTwoWithoutCallingMarkAsRead()
    {
        var called = false;

        var exitCode = MarkReadCommand.ExecuteCore(null, _ =>
        {
            called = true;
            return Task.FromResult(new List<(string Id, bool Success)>());
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exitCode, Is.EqualTo(2));
            Assert.That(called, Is.False);
        }
    }

    [Test]
    public void ExecuteCore_WithAllSuccesses_ReturnsExitCodeZero()
    {
        var exitCode = MarkReadCommand.ExecuteCore("1,2", ids =>
            Task.FromResult(ids.Select(i => (i, true)).ToList()));

        Assert.That(exitCode, Is.Zero);
    }

    [Test]
    public void ExecuteCore_WithSomeFailures_ReturnsExitCodeTwo()
    {
        var exitCode = MarkReadCommand.ExecuteCore("1,2", ids =>
            Task.FromResult(ids.Select((i, index) => (i, index == 0)).ToList()));

        Assert.That(exitCode, Is.EqualTo(2));
    }

    [Test]
    public void ExecuteCore_WithIdsThatParseToEmptyList_ReturnsExitCodeTwoWithoutCallingMarkAsRead()
    {
        var called = false;

        var exitCode = MarkReadCommand.ExecuteCore(", ,", _ =>
        {
            called = true;
            return Task.FromResult(new List<(string Id, bool Success)>());
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exitCode, Is.EqualTo(2));
            Assert.That(called, Is.False);
        }
    }

    [Test]
    public void ExecuteCore_WhenMarkAsReadThrowsConfigurationError_ReturnsExitCodeOne()
    {
        var exitCode = MarkReadCommand.ExecuteCore("1", _ =>
            throw new InvalidOperationException("No server configured"));

        Assert.That(exitCode, Is.EqualTo(1));
    }

    [Test]
    public void ExecuteCore_WhenMarkAsReadThrows_ReturnsExitCodeTwo()
    {
        var exitCode = MarkReadCommand.ExecuteCore("1", _ =>
            throw new InvalidOperationException("Connection failed"));

        Assert.That(exitCode, Is.EqualTo(2));
    }

    [Test]
    public void Create_WithUnreachableServer_InvokeReturnsExitCodeTwo()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var originalDir = Directory.GetCurrentDirectory();

        try
        {
            Directory.SetCurrentDirectory(tempDir);
            var config = new
            {
                Smtp = new
                {
                    Servers = new[]
                    {
                        new { Name = "unreachable", Address = "127.0.0.1", Port = 1, Username = "u", Password = "p", UseHttps = false }
                    }
                }
            };
            File.WriteAllText(Path.Combine(tempDir, "appsettings.json"), JsonSerializer.Serialize(config));

            var command = MarkReadCommand.Create();
            var exitCode = command.Parse(IdArgOne).Invoke();

            Assert.That(exitCode, Is.EqualTo(2));
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public void Create_ReturnsCommandWithIdAndServerNameOptions()
    {
        var command = MarkReadCommand.Create();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(command.Name, Is.EqualTo("mark-read"));
            Assert.That(command.Options.Select(o => o.Name), Does.Contain("--id"));
            Assert.That(command.Options.Select(o => o.Name), Does.Contain("--servername"));
        }
    }
}
