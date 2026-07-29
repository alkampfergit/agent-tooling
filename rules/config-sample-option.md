# Rule: Tool provides `--config-sample`

**Id:** config-sample-option
**Applies to:** every CLI tool under `src/` that reads configuration from the base config file

## Requirement

The tool must support a `--config-sample` option that prints a sample configuration
section explaining what to add to the base config file and how the tool is configured.
The output must be copy-paste ready: a valid config snippet with every setting the tool
reads, plus a comment or accompanying text for each setting explaining its purpose,
whether it is required, and its default value.

Tools that read no configuration at all are exempt — but must state so in their README.

## How to verify

From the repository root:

```powershell
dotnet run --project src/<ToolName> -- --config-sample
```

Cross-check the printed settings against the code: every configuration key the tool
actually reads (search the tool's source for config access) must appear in the sample,
and the sample must not list keys the tool never reads.

## Pass criteria

- [ ] `--config-sample` exits with code 0.
- [ ] Output is a syntactically valid snippet in the base config file's format.
- [ ] Every setting the tool reads appears in the sample.
- [ ] No settings appear that the tool does not read.
- [ ] Each setting has an explanation (purpose, required/optional, default value).
- [ ] `--help` lists the `--config-sample` option with a description.
