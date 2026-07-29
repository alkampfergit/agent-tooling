# Naming & design conventions (from Microsoft's System.CommandLine design guidance)

## Contents
- [Commands and subcommands](#commands-and-subcommands)
- [Naming](#naming)
- [Reserved short aliases](#reserved-short-aliases)
- [Verbs vs nouns](#verbs-vs-nouns)

## Commands and subcommands

If a command has subcommands, the command itself should be a grouping/area name, not
an action — e.g. `mytool cache clear`, not `mytool clear-cache`. A bare grouping
command (`mytool cache`) should error out and show help for its subcommands rather
than doing anything itself.

## Naming

- **kebab-case** for all option/argument/command names: `--additional-probing-path`.
- **lowercase only**; use aliases if case-insensitivity is needed.
- Keep names short and easy to spell — don't write `--classification` when `--class`
  is unambiguous.
- **Pluralization must be consistent**: if an option can repeat, pluralize it, and do
  so consistently across the tool (`--sources` and `--additional-probing-paths`, not
  a mix of singular/plural).

## Reserved short aliases

Don't repurpose these — users expect the .NET CLI meaning:

| Alias | Meaning |
|---|---|
| `-i` | `--interactive` |
| `-o` | `--output` (file path if one output, directory if many) |
| `-v` | `--verbosity` (bare `-v` = `Diagnostic`) |
| `-q` | `--verbosity Quiet` |
| `-c` | `--configuration` |
| `-f` | `--framework` |
| `-p` | `--property` |
| `-r` | `--runtime` |

## Verbs vs nouns

- **Commands** (actions) use verbs: `remove`, not `removal`.
- **Options** (parameters) use nouns: `--configuration`, not `--configure`.

## `--verbosity` option (if a tool needs one)

Standard five levels, abbreviations accepted: `Quiet`, `Minimal`, `Normal`,
`Detailed`, `Diagnostic`. A tool that only needs three can still define all five —
unused levels just map to the nearest defined behavior (e.g. `Minimal` == `Normal`).
If the tool supports `--interactive`, never let `--verbosity Quiet --interactive`
silently wait for input the user can't see — either still prompt, or reject the
combination.
