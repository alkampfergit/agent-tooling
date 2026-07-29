namespace Agent.Tools.Tests;

[TestFixture]
[Category("HelloTool")]
[Category("Unit")]
public class HelloToolTests
{
    [Test]
    public void NoArguments_PrintsDefaultGreeting()
    {
        var result = CliRunner.Run("HelloTool.dll");

        Assert.That(result.ExitCode, Is.EqualTo(0));
        Assert.That(result.StdOut, Does.Contain("Hello from Agent Tools!"));
    }

    [Test]
    public void Greet_PrintsGreetingForName()
    {
        var result = CliRunner.Run("HelloTool.dll", "greet", "World");

        Assert.That(result.ExitCode, Is.EqualTo(0));
        Assert.That(result.StdOut.Trim(), Is.EqualTo("Hello, World!"));
    }

    [Test]
    public void Greet_WithShout_PrintsUppercaseGreeting()
    {
        var result = CliRunner.Run("HelloTool.dll", "greet", "World", "--shout");

        Assert.That(result.ExitCode, Is.EqualTo(0));
        Assert.That(result.StdOut.Trim(), Is.EqualTo("HELLO, WORLD!"));
    }
}
