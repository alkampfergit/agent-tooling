namespace Agent.Tools.Tests;

[TestFixture]
[Category("hellotool")]
[Category("Unit")]
public class HelloToolTests
{
    [Test]
    public void NoArguments_PrintsDefaultGreeting()
    {
        var result = CliRunner.Run("HelloTool.dll");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ExitCode, Is.Zero);
            Assert.That(result.StdOut, Does.Contain("Hello from Agent Tools!"));
        }
    }

    [Test]
    public void Greet_PrintsGreetingForName()
    {
        var result = CliRunner.Run("HelloTool.dll", "greet", "World");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ExitCode, Is.Zero);
            Assert.That(result.StdOut.Trim(), Is.EqualTo("Hello, World!"));
        }
    }

    [Test]
    public void Greet_WithShout_PrintsUppercaseGreeting()
    {
        var result = CliRunner.Run("HelloTool.dll", "greet", "World", "--shout");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ExitCode, Is.Zero);
            Assert.That(result.StdOut.Trim(), Is.EqualTo("HELLO, WORLD!"));
        }
    }

    [Test]
    public void Greet_MissingNameArgument_ReturnsNonZeroExitCode()
    {
        var result = CliRunner.Run("HelloTool.dll", "greet");

        Assert.That(result.ExitCode, Is.Not.Zero);
    }

    [Test]
    public void Greet_WithMultiWordName_PrintsGreetingVerbatim()
    {
        var result = CliRunner.Run("HelloTool.dll", "greet", "John Doe");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ExitCode, Is.Zero);
            Assert.That(result.StdOut.Trim(), Is.EqualTo("Hello, John Doe!"));
        }
    }
}
