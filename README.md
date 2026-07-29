# Agent Tools

A collection of .NET CLI tools for GitHub agent operations. Each tool is designed with a single, narrow purpose.

## Quick Start

### Build All Tools

```powershell
./build.ps1
```

Options:
- `-Configuration Release` (default) or `Debug`
- `-Clean` to clean artifacts before building

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
