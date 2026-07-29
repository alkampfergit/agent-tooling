# HelloTool

Sample CLI tool demonstrating the repository's command-line parsing convention
(`System.CommandLine`).

## Usage

```powershell
# Print the default greeting
HelloTool

# Greet a specific name
HelloTool greet World

# Greet in uppercase
HelloTool greet World --shout

# Show help
HelloTool --help
```

## Commands

| Command | Arguments | Options | Description |
| --- | --- | --- | --- |
| *(root)* |  | `--shout` | Prints a default greeting from Agent Tools |
| `greet` | `name` | `--shout` | Prints a greeting for `name` |
