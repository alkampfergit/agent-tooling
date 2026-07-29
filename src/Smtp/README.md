# SMTP Email Tool

A minimal CLI tool for reading and managing emails via IMAP protocol. Optimized for low token consumption with LLMs.

## Quick Start

### View Help
```bash
smtp --help                    # Show all commands
smtp summary --help            # Get-details for summary command
smtp get-details --help        # Help for get-details command
smtp mark-read --help          # Help for mark-read command
```

### Configure
Create `appsettings.json` in your working directory:

```json
{
  "Smtp": {
    "Servers": [
      {
        "Name": "primary",
        "Address": "imap.example.com",
        "Port": 993,
        "Username": "user@example.com",
        "Password": "app-password",
        "UseHttps": true
      }
    ]
  }
}
```

Or place in a parent directory as `agent-tooling.json` for shared configuration across all tools.

### Usage

**List unread emails:**
```bash
smtp summary                    # Auto-select if only one server
smtp summary --servername primary
```

Output:
```json
[
  {
    "id": "123",
    "from": "sender@example.com",
    "subject": "Meeting tomorrow",
    "date": "2026-01-15T10:30:00Z",
    "preview": "Let's discuss the project..."
  }
]
```

**Get full email details:**
```bash
smtp get-details --id 123
smtp get-details --id 123 --servername primary
```

Output:
```json
{
  "id": "123",
  "from": "sender@example.com",
  "to": ["recipient@example.com"],
  "cc": [],
  "subject": "Meeting tomorrow",
  "date": "2026-01-15T10:30:00Z",
  "body": "Let's discuss the project plan..."
}
```

**Mark as read:**
```bash
smtp mark-read --id 123
smtp mark-read --id 123 --servername primary
```

Output:
```json
{
  "success": true,
  "id": "123"
}
```

## Help System

Each command has built-in help with `--help` or `-h`:

```bash
$ smtp --help
Description:
  SMTP email CLI tool for reading and managing emails via IMAP protocol. Configuration via appsettings.json or agent-tooling.json

Usage:
  Smtp [command] [options]

Options:
  -?, -h, --help  Show help and usage information
  --version       Show version information

Commands:
  summary      List unread emails from inbox
  get-details  Retrieve full email details by ID with HTML-to-text conversion
  mark-read    Mark an email as read
```

### Command Help

```bash
$ smtp summary --help
Description:
  List unread emails from inbox

Usage:
  Smtp summary [options]

Options:
  --servername <servername>  Name of the configured server (optional if only one server configured)
  -?, -h, --help             Show help and usage information
```

## Configuration

### File Location & Hierarchy
The tool searches for configuration in this order (first match wins):
1. **`appsettings.json`** in current working directory (tool-specific, overrides shared config)
2. **`agent-tooling.json`** in parent directories (up to filesystem root, shared across all tools)

This allows both per-tool configuration and shared/organizational configuration.

### Example: Per-Tool Configuration (appsettings.json)
Place in the directory where you run the tool:

```json
{
  "Smtp": {
    "Servers": [
      {
        "Name": "primary",
        "Address": "imap.example.com",
        "Port": 993,
        "Username": "user@example.com",
        "Password": "app-password",
        "UseHttps": true
      }
    ]
  }
}
```

### Example: Shared Configuration (agent-tooling.json)
Place in a parent directory (e.g., project root) to share across all tools:

```json
{
  "Smtp": {
    "Servers": [
      {
        "Name": "work",
        "Address": "imap.company.com",
        "Port": 993,
        "Username": "user@company.com",
        "Password": "corp-password",
        "UseHttps": true
      },
      {
        "Name": "personal",
        "Address": "imap.gmail.com",
        "Port": 993,
        "Username": "user@gmail.com",
        "Password": "app-password",
        "UseHttps": true
      }
    ]
  }
}
```

### Server Configuration Fields

**Required fields:**
- `Name`: Identifier for the server (used with `--servername`)
- `Address`: IMAP server hostname
- `Port`: IMAP port (typically 993 for IMAPS)
- `Username`: Email account username
- `Password`: Email account password
- `UseHttps`: `true` for secure connection (recommended)

### Server Selection
- **Single server**: `--servername` is optional; auto-selects the only configured server
- **Multiple servers**: `--servername` is required; tool lists available servers if missing
- **No servers**: Error with helpful message

## Exit Codes

- **0**: Success
- **1**: Configuration error (missing config, server not found, etc.)
- **2**: Runtime error (authentication failed, email not found, IMAP error)

## Output Format

All output is minimal JSON (single-line or pretty-printed) optimized for LLM token consumption:

- **No extra fields**: Only requested data is returned
- **Flat structure**: No nested objects unless necessary
- **Compact keys**: Short JSON property names
- **ISO 8601 dates**: Standard format for timestamps

## HTML to Text Conversion

When fetching email details with HTML content:
- HTML tags are completely stripped
- Script and style blocks are removed
- Whitespace is normalized (multiple spaces/newlines → single space)
- Only text content is preserved

## Limitations

- **Read-only for now**: View and mark-read only; no send/compose
- **IMAP only**: No SMTP sending (despite the name)
- **Single folder**: Inbox only; no folder navigation
- **No attachments**: Attachment metadata ignored
- **No filtering**: Get all unread; custom filters not supported

## Testing

Run tests (integration tests marked `[Explicit]` require mailtrap):

```bash
dotnet test src/Agent.Tools.Tests/Agent.Tools.Tests.csproj
```

Unit tests (always run):
- HTML-to-text conversion
- Configuration loading and server selection
- Model serialization

Integration tests (require configured mailtrap):
- Email fetching
- Mark-as-read operations

## Development

**Build:**
```bash
dotnet build src/Agent.Tools.sln
```

**Test:**
```bash
dotnet test src/Agent.Tools.Tests/Agent.Tools.Tests.csproj
```

**Run:**
```bash
dotnet run --project src/Smtp/Smtp.csproj -- summary
```

**Project Structure:**
```
src/Smtp/
├── Program.cs                    # CLI entry point
├── Configuration/
│   ├── ConfigurationProvider.cs  # Config loading & server selection
│   ├── ServerConfig.cs           # Server configuration model
│   └── SmtpConfiguration.cs      # Root config model
├── Models/
│   ├── EmailSummary.cs           # List response model
│   ├── EmailDetails.cs           # Details response model
│   └── MarkReadResult.cs         # Success indicator model
├── Services/
│   ├── EmailService.cs           # Core IMAP operations
│   ├── ImapClientFactory.cs      # MailKit connection management
│   └── HtmlToTextConverter.cs    # HTML stripping utility
├── Commands/
│   ├── SummaryCommand.cs         # summary subcommand
│   ├── GetDetailsCommand.cs      # get-details subcommand
│   └── MarkReadCommand.cs        # mark-read subcommand
└── docs/
    └── specs.md                  # Specification document
```

## Dependencies

- **MailKit 4.7.0**: IMAP protocol implementation
- **HtmlAgilityPack 1.11.59**: HTML parsing and stripping
- **Microsoft.Extensions.Configuration**: Config file loading
- **System.CommandLine 2.0.0**: CLI framework

## License

Part of the Agent Tools repository.
