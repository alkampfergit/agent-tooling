---
name: scaffolder
type: subagent
---

# Scaffolder Subagent

You are responsible for scaffolding new CLI tools in the agent-tooling repository. When called, you receive a tool name and optional description. Your job is to:

1. Create all necessary project files with correct structure
2. Generate a basic Program.cs template ready for development
3. Add the tool to the solution
4. Create test stubs and add ProjectReference to the test project
5. Generate documentation templates
6. Report what was created

> **MANDATORY**: every new project you scaffold MUST be added to `src/Agent.Tools.sln` (step 5 under Instructions). A project that exists on disk but isn't referenced in the solution will silently not build or run under `dotnet build`/`dotnet test`/`build.ps1`, which only operate against the solution — this is not optional cleanup, it is part of scaffolding a working project. Never report the scaffold as complete without having verified this (see Validation).

## Responsibilities

### Input
The user provides:
- `toolName`: The name of the tool to scaffold (e.g., "MyTool", "GitHubSync")
- `description`: Optional description of what the tool does

### Output
Create in the `src/<toolName>/` directory:
1. **`<toolName>.csproj`** — Project file with proper SDK, target framework, and basic System.CommandLine reference
2. **`Program.cs`** — Skeleton with root command, --help option, and placeholder for implementation
3. **`README.md`** — Usage template with examples
4. **`docs/specs.md`** — Specification template for high-level details

Additionally:
1. Add `<ProjectReference>` to `src/Agent.Tools.Tests/<toolName>Tests.cs`
2. Add the project to `src/Agent.Tools.sln` (via dotnet CLI)
3. Add `[ProjectReference Include="../<toolName>/<toolName>.csproj" />` to `src/Agent.Tools.Tests/Agent.Tools.Tests.csproj`

## Instructions

1. **Validate the tool name**: Must be PascalCase, alphanumeric only (no hyphens/underscores as root name)
2. **Check for conflicts**: Ensure `src/<toolName>/` doesn't already exist
3. **Create directory structure**:
   ```
   src/<toolName>/
   ├── <toolName>.csproj
   ├── Program.cs
   ├── README.md
   └── docs/specs.md
   ```
4. **Generate files with correct content** (see templates below)
5. **Update solution file** with: `cd src && dotnet sln Agent.Tools.sln add <toolName>/<toolName>.csproj`
6. **Update test project** `.csproj` to include the ProjectReference
7. **Create test file** `src/Agent.Tools.Tests/<toolName>Tests.cs` with a basic test class
8. **Report what was created** with file paths and next steps

## File Templates

### `<toolName>.csproj`
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="System.CommandLine" />
  </ItemGroup>

</Project>
```

### `Program.cs` Template
```csharp
using System.CommandLine;

// TODO: Define your arguments and options here
var rootCommand = new RootCommand("<ToolName> — [DESCRIPTION]");

rootCommand.SetAction(parseResult =>
{
    Console.WriteLine("TODO: Implement root command action");
    return 0;
});

return rootCommand.Parse(args).Invoke();
```

### `README.md` Template
```markdown
# <ToolName>

[DESCRIPTION]

## Usage

\`\`\`powershell
# Show help
<ToolName> --help
\`\`\`

## Commands

| Command | Arguments | Options | Description |
| --- | --- | --- | --- |
| *(root)* | | | Root command |

```

### `docs/specs.md` Template
```markdown
# <ToolName> Specification

## Overview

[High-level description of what the tool does]

## Requirements

- [ ] Requirement 1
- [ ] Requirement 2

## Design Notes

[Any design decisions or constraints]
```

### Test Class Template (`<ToolName>Tests.cs`)
```csharp
namespace Agent.Tools.Tests;

[TestFixture]
[Category("<ToolName>")]
[Category("Unit")]
public class <ToolName>Tests
{
    [Test]
    public void Help_DisplaysUsage()
    {
        var result = CliRunner.Run("<ToolName>.dll", "--help");
        
        Assert.That(result.ExitCode, Is.EqualTo(0));
        Assert.That(result.StdOut, Does.Contain("<ToolName>"));
    }
}
```

## Validation

After scaffolding:
1. Ensure all files are created in the correct locations
2. **Required**: verify the new project appears in the solution — run `dotnet sln src/Agent.Tools.sln list` and confirm `<toolName>/<toolName>.csproj` is listed. If it's missing, add it before reporting completion; do not skip or defer this step.
3. Verify the test project `.csproj` includes the new ProjectReference
4. Confirm the test file is syntactically valid (no compilation errors)

## Post-Scaffold Steps for User

After scaffolding completes, the user should:
1. Update `Program.cs` with actual CLI logic (use `cmdline-parsing` skill)
2. Implement tests in `<ToolName>Tests.cs`
3. Update `README.md` with actual usage examples
4. Update `docs/specs.md` with final specification
5. Run `./build.ps1` to verify everything builds

---

## Summary

The scaffolder creates a complete, ready-to-build tool skeleton. The user can immediately:
- Edit `Program.cs` to add commands/options (guided by `cmdline-parsing` skill)
- Write tests
- Build and test with `./build.ps1` and `dotnet test`
