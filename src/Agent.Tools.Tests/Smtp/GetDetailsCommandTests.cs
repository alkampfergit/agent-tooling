namespace Agent.Tools.Tests.Smtp;

using global::Smtp.Commands;
using global::Smtp.Models;

[TestFixture]
[Category("smtp")]
[Category("Unit")]
public class GetDetailsCommandTests
{
    private static EmailDetails MakeDetails(string id)
    {
        return new EmailDetails
        {
            Id = id,
            From = "sender@example.com",
            Subject = "Subject",
            Date = DateTime.UtcNow,
            Body = "Body"
        };
    }

    [Test]
    public void ExecuteCore_WithMissingId_ReturnsExitCodeTwoWithoutCallingGetDetails()
    {
        var called = false;

        var exitCode = GetDetailsCommand.ExecuteCore(null, _ =>
        {
            called = true;
            return Task.FromResult<EmailDetails?>(null);
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exitCode, Is.EqualTo(2));
            Assert.That(called, Is.False);
        }
    }

    [Test]
    public void ExecuteCore_WithValidId_ReturnsExitCodeZero()
    {
        var exitCode = GetDetailsCommand.ExecuteCore("42", id => Task.FromResult<EmailDetails?>(MakeDetails(id)));

        Assert.That(exitCode, Is.EqualTo(0));
    }

    [Test]
    public void ExecuteCore_WithNotFoundId_ReturnsExitCodeTwo()
    {
        var exitCode = GetDetailsCommand.ExecuteCore("999999", _ => Task.FromResult<EmailDetails?>(null));

        Assert.That(exitCode, Is.EqualTo(2));
    }

    [Test]
    public void ExecuteCore_WhenGetDetailsThrowsConfigurationError_ReturnsExitCodeOne()
    {
        var exitCode = GetDetailsCommand.ExecuteCore("1", _ =>
            throw new InvalidOperationException("No servers configured"));

        Assert.That(exitCode, Is.EqualTo(1));
    }

    [Test]
    public void ExecuteCore_WhenGetDetailsThrows_ReturnsExitCodeTwo()
    {
        var exitCode = GetDetailsCommand.ExecuteCore("1", _ =>
            throw new InvalidOperationException("Connection failed"));

        Assert.That(exitCode, Is.EqualTo(2));
    }

    [Test]
    public void Create_ReturnsCommandWithIdAndServerNameOptions()
    {
        var command = GetDetailsCommand.Create();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(command.Name, Is.EqualTo("get-details"));
            Assert.That(command.Options.Select(o => o.Name), Does.Contain("--id"));
            Assert.That(command.Options.Select(o => o.Name), Does.Contain("--servername"));
        }
    }
}
