---
name: cmdline-parsing
description: Parse command-line arguments in any .NET tool in this repo using System.CommandLine, the standard library for all tools under src/. Use whenever creating a new tool, adding an option/argument/subcommand to an existing tool, or when the user asks to "parse arguments", "add a --flag", "add a subcommand", or "handle cmdline input". Do NOT hand-roll parsing of the raw `args` array or use a different CLI library — every tool in this repo must parse the same way for a consistent UX.
---

Every CLI tool in this repo (`src/<tool_name>/`) parses its command line with
**System.CommandLine**, Microsoft's official library. This keeps `--help`, error
messages, and option/argument behavior identical across all tools.

## Setup (once per tool)

1. Ensure the package version is centralized in `src/Directory.Packages.props`:
   ```xml
   <PackageVersion Include="System.CommandLine" Version="2.0.0" />
   ```
2. Reference it from the tool's `.csproj` with **no version** (centrally managed):
   ```xml
   <ItemGroup>
     <PackageReference Include="System.CommandLine" />
   </ItemGroup>
   ```
3. `Program.cs` uses top-level statements, ending with:
   ```csharp
   return rootCommand.Parse(args).Invoke();
   ```

## Standing rules

- One `RootCommand` per tool. Only add subcommands if the tool genuinely has more
  than one action; a single-purpose tool just uses options/arguments on the root.
- Every `Option<T>` / `Argument<T>` gets a `Description`.
- Prefer letting `Invoke()` handle parse errors and `--help`/`--version` — don't
  write custom argument-count checks or manual error printing.
- Validate values that need more than type-conversion (e.g. file existence) via
  `CustomParser`, not after parsing completes.
- Follow naming conventions in [references/design-guidance.md](references/design-guidance.md)
  (kebab-case, verbs for commands, nouns for options, consistent pluralization) —
  read it before naming a new option or subcommand.

## Example 1 — simple tool, options only

```csharp
using System.CommandLine;

Option<string> nameOption = new("--name")
{
    Description = "Name to greet",
    DefaultValueFactory = _ => "World"
};
Option<bool> shoutOption = new("--shout")
{
    Description = "Print the greeting in uppercase"
};

RootCommand rootCommand = new("Greets someone")
{
    nameOption,
    shoutOption
};

rootCommand.SetAction(parseResult =>
{
    var name = parseResult.GetValue(nameOption);
    var shout = parseResult.GetValue(shoutOption);
    var message = $"Hello, {name}!";
    Console.WriteLine(shout ? message.ToUpperInvariant() : message);
    return 0;
});

return rootCommand.Parse(args).Invoke();
```

## Example 2 — tool with subcommands

```csharp
using System.CommandLine;

Argument<string> nameArgument = new("name") { Description = "Name to greet" };
Option<bool> shoutOption = new("--shout") { Description = "Uppercase output", Recursive = true };

Command greetCommand = new("greet", "Print a greeting")
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

Command versionCommand = new("about", "Show tool info");
versionCommand.SetAction(_ =>
{
    Console.WriteLine("HelloTool - example CLI");
    return 0;
});

RootCommand rootCommand = new("Example multi-command tool")
{
    Options = { shoutOption },
    Subcommands = { greetCommand, versionCommand }
};

return rootCommand.Parse(args).Invoke();
```

`Recursive = true` makes `shoutOption` available on every subcommand, not just the root.
