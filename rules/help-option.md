# Rule: Tool provides `--help`

**Id:** help-option
**Applies to:** every CLI tool under `src/`

## Requirement

The tool must support a `--help` option (and `-h`/`-?` aliases as provided by
System.CommandLine) that prints usage information: a description of the tool,
its commands/subcommands, and each option with a short explanation.

## How to verify

From the repository root:

```powershell
dotnet run --project src/<ToolName> -- --help
```

For tools with subcommands, also verify help on at least one subcommand:

```powershell
dotnet run --project src/<ToolName> -- <subcommand> --help
```

## Pass criteria

- [ ] `--help` exits with code 0.
- [ ] Output includes a non-empty tool description (not just the assembly name).
- [ ] Output lists every command and option the tool exposes.
- [ ] Every option has a description string (no blank descriptions).
- [ ] Subcommands (if any) also respond to `--help` with their own usage text.
