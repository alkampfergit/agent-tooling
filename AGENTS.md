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
5. Keep a README.md in the tool's directory with usage instructions
6. Keep a docs/specs.md with details of the hith level specification of the tool

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

# General rule to write code

## 1. Think Before Coding

**Don't assume. Don't hide confusion. Surface tradeoffs.**

Before implementing:
- State your assumptions explicitly. If uncertain, ask.
- If multiple interpretations exist, present them - don't pick silently.
- If a simpler approach exists, say so. Push back when warranted.
- If something is unclear, stop. Name what's confusing. Ask.

## 2. Simplicity First

**Minimum code that solves the problem. Nothing speculative.**

- No features beyond what was asked.
- No abstractions for single-use code.
- No "flexibility" or "configurability" that wasn't requested.
- No error handling for impossible scenarios.
- If you write 200 lines and it could be 50, rewrite it.

Ask yourself: "Would a senior engineer say this is overcomplicated?" If yes, simplify.

## 3. Surgical Changes

**Touch only what you must. Clean up only your own mess.**

When editing existing code:
- Don't "improve" adjacent code, comments, or formatting.
- Don't refactor things that aren't broken.
- Match existing style, even if you'd do it differently.
- If you notice unrelated dead code, mention it - don't delete it.

When your changes create orphans:
- Remove imports/variables/functions that YOUR changes made unused.
- Don't remove pre-existing dead code unless asked.

The test: Every changed line should trace directly to the user's request.

## 4. Goal-Driven Execution

**Define success criteria. Loop until verified.**

Transform tasks into verifiable goals:
- "Add validation" → "Write tests for invalid inputs, then make them pass"
- "Fix the bug" → "Write a test that reproduces it, then make it pass"
- "Refactor X" → "Ensure tests pass before and after"

For multi-step tasks, state a brief plan:
```
1. [Step] → verify: [check]
2. [Step] → verify: [check]
3. [Step] → verify: [check]
