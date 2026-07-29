# Agent Tools Repository

This repository contains a collection of .NET CLI tools for GitHub agent operations. Each tool is designed with a single, narrow purpose.

## Repository Structure

- `src/` — Contains all CLI tools and shared project configuration
- `src/Agent.Tools.sln` — Solution file for all tools
- `src/Directory.Packages.props` — Centralized NuGet package versioning
- `tools/` — Output directory for built binaries and dependencies (generated during build)

## Adding a New Tool

1. Create a new directory in `src/<tool_name>/`
2. Add a `.csproj` file if the tool is complex, or create a single `.cs` file with implicit Program.cs for simple tools
3. Reference packages from `Directory.Packages.props` in your project
4. Add the project reference to `Agent.Tools.sln`

### Project Template

**For simple tools (single executable):**
```
src/<tool_name>/<tool_name>.csproj
```

**For complex tools (multiple files):**
```
src/<tool_name>/
├── <tool_name>.csproj
├── Program.cs
└── [other source files]
```

## Building

Use the build script to compile all tools and prepare the `tools/` directory:

```powershell
./build.ps1
```

This will:
- Build all projects in the solution
- Copy all tool binaries to `tools/`
- Copy all NuGet dependencies to `tools/`
- Maintain dependency versions as defined in `Directory.Packages.props`

## Package Management

All NuGet package versions are managed centrally in `Directory.Packages.props`. This ensures:
- No version conflicts between tools
- Simplified dependency updates
- Single source of truth for package versions
