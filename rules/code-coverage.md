# Rule: Minimum test line coverage

**Id:** code-coverage
**Applies to:** every CLI tool under `src/` that has tests in `src/Agent.Tools.Tests/`

## Requirement

SonarCloud's quality gate requires 80% coverage on new code. New-code coverage can only be
computed exactly by SonarCloud itself (it needs the "new code" period definition and its own
diff analysis), so this rule does not attempt to reproduce that number precisely. Instead it
gives an **absolute, locally-reproducible line-coverage number per tool** — always report a
real number, never "coverage not checked" — plus a best-effort approximation of coverage on
the lines actually changed in the current branch, which tracks the intent of the SonarCloud
gate closely enough to catch the same class of problem before a PR is pushed.

Both numbers must be reported. Only the **overall tool line-rate** gates PASS/FAIL (see Pass
criteria) since it is exact; the changed-lines figure is informational context for the report,
not a hard gate, precisely because it is an approximation.

## How to verify

### 1. Generate a coverage report

From the repository root:

```powershell
dotnet test src/Agent.Tools.Tests/Agent.Tools.Tests.csproj `
  --filter "Category=<toolname>&Category!=Integration" `
  --collect:"XPlat Code Coverage" `
  --results-directory src/Agent.Tools.Tests/TestResults
```

This uses `coverlet.collector` (already referenced by `Agent.Tools.Tests.csproj`) and needs no
extra tool install. It emits a Cobertura XML file under
`src/Agent.Tools.Tests/TestResults/<run-guid>/coverage.cobertura.xml`. Find the newest one:

```powershell
Get-ChildItem -Path "src/Agent.Tools.Tests/TestResults" -Filter "coverage.cobertura.xml" -Recurse |
  Sort-Object LastWriteTime -Descending | Select-Object -First 1
```

### 2. Read the absolute per-tool line-rate (exact, no extra tooling needed)

Cobertura groups coverage by `<package name="...">`, and each tool's assembly name is its own
package. Read the `line-rate` attribute directly — it is already a 0–1 fraction, multiply by
100 for a percentage:

```powershell
Select-String -Path $coverageFile -Pattern '<package name="<ToolName>" line-rate="([^"]*)"'
```

This is the absolute number to report even when a precise new-code figure isn't available —
never report "unknown" when this command can run.

### 3. Approximate new-code coverage (best effort, informational only)

Cross-reference the lines changed in this branch against the same Cobertura report's per-line
`hits` data, since both are keyed by file and line number:

```powershell
# Lines added/modified by this branch, per file, with line numbers
git diff master...HEAD --unified=0 -- src/<ToolName> | Select-String '^\+\+\+ b/|^@@'
```

For each changed file, parse the Cobertura `<class filename="...">` block's `<line number="N"
hits="H">` entries, keep only the line numbers that appear in the diff's added-line ranges, and
compute `(count where hits > 0) / (total changed lines that are executable)`. Lines the diff
touched that don't appear in the Cobertura report at all (e.g. comments, blank lines, non-code
files) are excluded from both the numerator and denominator — Cobertura only lists executable
lines, so this filtering happens automatically by using its line list as the source of truth for
"what counts as code."

This is an approximation, not what SonarCloud reports: it can't reproduce SonarCloud's own
new-code period boundary. Report it as "approximate new-code coverage: X% (N/M changed
executable lines hit)" — never as if it were the SonarCloud number.

## Pass criteria

- [ ] A Cobertura coverage report was generated successfully (command above exits 0 and
      produces a `coverage.cobertura.xml`).
- [ ] The tool's `<package name="<ToolName>">` line-rate was read and reported as a percentage
      — this absolute number must always be present in the report, pass or fail.
- [ ] **Overall tool line-rate ≥ 80%.** This is the gating criterion.
- [ ] The best-effort approximate new-code coverage was computed and reported alongside the
      absolute number (informational — does not by itself cause FAIL, but must not be omitted).
- [ ] If overall line-rate is below 80%, the report names the specific classes/methods with the
      lowest `line-rate` in the tool's package (from the Cobertura `<class>` entries) so the
      gap is actionable, not just a single number.
