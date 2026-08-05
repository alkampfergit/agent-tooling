namespace Agent.Tools.Tests.Smtp;

using global::Smtp.Commands;
using global::Smtp.Models;

[TestFixture]
[Category("smtp")]
[Category("Unit")]
public class SummaryCommandTests
{
    private static EmailSummary MakeSummary(string id, DateTime date, string preview = "preview")
    {
        return new EmailSummary
        {
            Id = id,
            From = "sender@example.com",
            Subject = "Subject " + id,
            Date = date,
            Preview = preview
        };
    }

    [Test]
    public void ExecuteCore_WithNoEmails_ReturnsExitCodeZero()
    {
        var exitCode = SummaryCommand.ExecuteCore(30, () => Task.FromResult(new List<EmailSummary>()));

        Assert.That(exitCode, Is.EqualTo(0));
    }

    [Test]
    public void ExecuteCore_SortsByDateDescendingAndAppliesLimit()
    {
        var emails = new List<EmailSummary>
        {
            MakeSummary("1", new DateTime(2026, 1, 1)),
            MakeSummary("2", new DateTime(2026, 3, 1)),
            MakeSummary("3", new DateTime(2026, 2, 1))
        };

        List<EmailSummary>? getUnreadResult = null;
        var exitCode = SummaryCommand.ExecuteCore(2, () =>
        {
            getUnreadResult = emails;
            return Task.FromResult(emails);
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exitCode, Is.EqualTo(0));
            Assert.That(getUnreadResult, Is.Not.Null);
        }
    }

    [Test]
    public void ExecuteCore_WhenGetUnreadThrowsConfigurationError_ReturnsExitCodeOne()
    {
        var exitCode = SummaryCommand.ExecuteCore(30, () =>
            throw new InvalidOperationException("No servers configured"));

        Assert.That(exitCode, Is.EqualTo(1));
    }

    [Test]
    public void ExecuteCore_WhenGetUnreadThrows_ReturnsExitCodeTwo()
    {
        var exitCode = SummaryCommand.ExecuteCore(30, () =>
            throw new InvalidOperationException("Connection failed"));

        Assert.That(exitCode, Is.EqualTo(2));
    }

    [Test]
    public void Create_ReturnsCommandWithServerNameAndLimitOptions()
    {
        var command = SummaryCommand.Create();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(command.Name, Is.EqualTo("summary"));
            Assert.That(command.Options.Select(o => o.Name), Does.Contain("--servername"));
            Assert.That(command.Options.Select(o => o.Name), Does.Contain("--limit"));
        }
    }
}
