# Rule: Shared code lives in Agent.Tools.Common

**Id:** common-code-placement
**Applies to:** every CLI tool under `src/`

## Requirement

Code inside a tool's project must be specific to that tool. Anything general-purpose —
infrastructure, utilities, or logic that another tool already has or would plausibly
need — belongs in `src/Agent.Tools.Common/` instead. In particular a tool must not:

- Reimplement functionality that already exists in `Agent.Tools.Common`
  (e.g. hand-rolled config loading instead of `ConfigurationManager.LoadToolConfiguration`).
- Contain a copy of code that also exists in another tool under `src/` (duplication
  across tools is the signal that the code should be promoted to Common).
- Define general-purpose helpers with no tool-specific knowledge (string/path/process
  utilities, retry wrappers, output formatting helpers, etc.) inside the tool project.

## How to verify

1. List what `Agent.Tools.Common` currently provides (public classes in
   `src/Agent.Tools.Common/*.cs`).
2. Read every `.cs` file in `src/<ToolName>/` and, for each class/method, ask:
   - Does it duplicate something already in Common? → violation.
   - Does near-identical code exist in another tool under `src/`? Search the other
     tool directories for similar signatures/logic. → violation (promote to Common).
   - Is it generic (would compile and make sense with no reference to this tool's
     domain)? → candidate violation; use judgment for trivial one-off snippets
     (< ~10 lines used once is acceptable inline).

## Pass criteria

- [ ] The tool uses `Agent.Tools.Common` facilities where they exist instead of
      reimplementing them (config loading via `ConfigurationManager`, etc.).
- [ ] No class or method in the tool is duplicated (identically or near-identically)
      in another tool under `src/`.
- [ ] No non-trivial general-purpose helper (no tool-specific knowledge, reusable
      as-is) lives in the tool project.

When this rule fails, the report must name the specific class/method, its file and
line, and state whether the fix is "use existing Common API X" or "move to
Agent.Tools.Common as new shared code".
