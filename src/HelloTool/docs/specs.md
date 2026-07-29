# HelloTool — Specification

## Purpose

Sample/reference tool for the Agent Tools repository. Demonstrates the standard
command-line parsing pattern (`System.CommandLine`) that all tools in this repo
follow, including root-command behavior, a subcommand, and a recursive option.

## Behavior

- Invoked with no arguments: prints a fixed greeting identifying the tool.
- `greet <name>`: prints `Hello, <name>!`.
- `--shout`: recursive option (available on root and all subcommands); when set,
  the printed greeting is uppercased instead of mixed-case.

## Exit codes

- `0`: success.
- Non-zero: parse error (handled automatically by `System.CommandLine`).
