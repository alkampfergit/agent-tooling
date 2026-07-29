# Rule: Test classes must have tool name in lowercase Category

**Id:** test-categories
**Applies to:** every CLI tool under `src/` that has tests in `src/Agent.Tools.Tests/`

## Requirement

Every test class must carry `[Category("<toolname>")]` where the tool name is in lowercase,
and an additional `[Category("Unit")]` or `[Category("Integration")]` to distinguish test type.
This allows tests for one tool to be run in isolation and filtered by type.

## How to verify

From the repository root:

```powershell
# Search for all test classes in the tool's test files
Get-ChildItem -Path "src/Agent.Tools.Tests/" -Filter "*Tests.cs" -Recurse | Where-Object { $_ -match "<ToolName>" }
```

Then inspect each test class in those files for the required Category attributes.

## Pass criteria

- [ ] Every test class has `[Category("<toolname>")]` attribute where toolname is the lowercase version of the tool name (e.g., `[Category("smtp")]` for the Smtp tool, `[Category("hellotool")]` for the HelloTool).
- [ ] Every test class also has `[Category("Unit")]` or `[Category("Integration")]` attribute.
- [ ] The lowercase category allows running tests with `dotnet test --filter "Category=<toolname>"`.
- [ ] Integration tests can be filtered out with `dotnet test --filter "Category!=Integration"`.
