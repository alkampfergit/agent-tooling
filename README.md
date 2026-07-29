# Agent Tools

A collection of .NET CLI tools for GitHub agent operations. Each tool is designed with a single, narrow purpose.

## Quick Start

### Build All Tools

```powershell
./build.ps1
```

Options:
- `-Configuration Release` (default) or `Debug`
- `-Clean` to clean build artifacts from src/

Output: Built executables in `tools/` directory (flattened, no framework subfolder)

**Important:** The `tools/` directory is cleaned and rebuilt from scratch each time. Copy `appsettings.json` to `tools/` if you want to run tools from that directory (see Configuration section below).

### Run Tools Directly from Source

Instead of building and distributing, you can run tools directly using `dotnet run`:

**Smtp tool example:**
```bash
cd src/Smtp

# List unread emails (default 30, newest first)
dotnet run -- summary

# With server selection
dotnet run -- summary --servername primary

# Custom limit
dotnet run -- summary --limit 50

# Get email details
dotnet run -- get-details --id 123

# Mark as read
dotnet run -- mark-read --id 123

# View help
dotnet run -- --help
dotnet run -- summary --help
```

**HelloTool example:**
```bash
cd src/HelloTool

# Greet someone
dotnet run -- greet "World"

# Uppercase output
dotnet run -- greet "World" --shout

# View help
dotnet run -- --help
```

**Running from tools/ directory:**

After building with `build.ps1`, you can run executables directly from `tools/`:

```bash
# Copy your configuration to tools/
cp appsettings.json tools/

# Run from tools directory
cd tools
.\Smtp.exe summary
.\Smtp.exe summary --limit 50
```

**Notes:**
- `dotnet run` requires .NET SDK installed and runs from source
- Tools in `tools/` directory are fully built and need no SDK
- Configuration files must be in the working directory when running tools
- Dependencies are included in `tools/` directory
- Use `--` to separate dotnet arguments from tool arguments (when using `dotnet run`)

### Project Structure

```
agent-tooling/
├── src/
│   ├── Agent.Tools.sln          # Solution file
│   ├── Directory.Packages.props  # Centralized NuGet versions
│   └── <tool-name>/             # Individual tool projects
│       ├── <tool-name>.csproj
│       └── Program.cs
├── tools/                         # Output directory (generated)
├── build.ps1                      # Build script
└── AGENTS.md                      # Repository guidelines
```

## Creating a New Tool

1. Create a directory in `src/<tool-name>/`
2. Create a `.csproj` file:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>
</Project>
```

3. Create `Program.cs` with your CLI logic
4. Add project reference to `Agent.Tools.sln`:

```bash
cd src
dotnet sln Agent.Tools.sln add <tool-name>/<tool-name>.csproj
```

## Managing Dependencies

All NuGet package versions are defined in `src/Directory.Packages.props`. To add a package:

```xml
<ItemGroup>
  <PackageVersion Include="PackageName" Version="1.0.0" />
</ItemGroup>
```

Then reference it in your `.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="PackageName" />
</ItemGroup>
```

## Build Output

The `build.ps1` script produces a `tools/` directory containing:
- All built executables
- All NuGet dependencies (flattened by version)
- Subdirectories per tool if needed

This makes it easy to distribute or deploy all tools together.
