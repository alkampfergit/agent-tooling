using System.CommandLine;

Argument<string> nameArgument = new("name")
{
    Description = "Name to greet"
};
Option<bool> shoutOption = new("--shout")
{
    Description = "Print the greeting in uppercase",
    Recursive = true
};

Command greetCommand = new("greet", "Print a greeting for someone")
{
    nameArgument
};
greetCommand.SetAction(parseResult =>
{
    var name = parseResult.GetValue(nameArgument);
    var shout = parseResult.GetValue(shoutOption);
    var message = $"Hello, {name}!";
    Console.WriteLine(shout ? message.ToUpperInvariant() : message);
    return 0;
});

RootCommand rootCommand = new("Sample tool for Agent Tools")
{
    Options = { shoutOption },
    Subcommands = { greetCommand }
};
rootCommand.SetAction(parseResult =>
{
    Console.WriteLine("Hello from Agent Tools!");
    Console.WriteLine("Tool: HelloTool");
    return 0;
});

return rootCommand.Parse(args).Invoke();
