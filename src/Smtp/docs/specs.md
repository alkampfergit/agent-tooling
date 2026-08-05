# SMTP Tool — Specification

## Purpose

Provides minimal email operations via IMAP and SMTP protocols using MailKit library. Designed for integration with AI/LLM workflows with minimal output token consumption. All configuration is externalized; no secrets are passed via CLI arguments.

## Configuration

Configuration is loaded in the following order (later sources override earlier):
1. `agent-tooling.json` (searched in parent directories, shared across tools)
2. `appsettings.json` (in current working directory, tool-specific)

Both files must contain a `Smtp` root key with server configuration:

```json
{
  "Smtp": {
    "Servers": [
      {
        "Name": "primary",
        "Address": "imap.example.com",
        "Port": 993,
        "Username": "user@example.com",
        "Password": "secret",
        "UseHttps": true
      }
    ]
  }
}
```

If only one server is configured, `--servername` is optional and defaults to that server.
If multiple servers are configured and `--servername` is omitted, the command fails with an error.

## Commands

### summary
**Description:** List unread emails from the inbox.

**Syntax:**
```
smtp summary [--servername <name>]
```

**Arguments:**
- (none)

**Options:**
- `--servername <name>` (optional): Name of the configured server. Defaults to the single configured server if only one exists.

**Output:** JSON array of unread emails (minimal format).
```json
[
  {
    "id": "1",
    "from": "sender@example.com",
    "subject": "Meeting tomorrow",
    "date": "2026-01-15T10:30:00Z",
    "preview": "Let's discuss the project plan tomorrow..."
  }
]
```

**Exit codes:**
- `0`: success
- `1`: configuration error (no servers configured, server not found, connection failed)
- `2`: runtime error (authentication failed, IMAP error)

---

### get-details
**Description:** Retrieve full email details by ID.

**Syntax:**
```
smtp get-details --id <id> [--servername <name>]
```

**Arguments:**
- (none)

**Options:**
- `--id <id>` (required): Email unique ID from summary command.
- `--servername <name>` (optional): Name of the configured server.

**Output:** JSON object with full email data.
```json
{
  "id": "1",
  "from": "sender@example.com",
  "to": ["recipient@example.com"],
  "cc": [],
  "subject": "Meeting tomorrow",
  "date": "2026-01-15T10:30:00Z",
  "body": "Let's discuss the project plan tomorrow at 2 PM. Please confirm your availability."
}
```

If the email has HTML content, it is converted to plain text using HtmlAgilityPack (all HTML tags stripped).

**Exit codes:**
- `0`: success
- `1`: configuration error
- `2`: runtime error (email not found, authentication failed)

---

### mark-read
**Description:** Mark one or more emails as read.

**Syntax:**
```
smtp mark-read --id <id[,id...]> [--servername <name>]
```

**Arguments:**
- (none)

**Options:**
- `--id <ids>` (required): Email unique ID, or a comma-separated list of IDs (e.g. `--id 1,2,3`). Whitespace around each ID is trimmed; empty entries are ignored.
- `--servername <name>` (optional): Name of the configured server.

**Behavior:** Each ID is processed independently (best-effort) against a single opened inbox connection — a failure on one ID does not prevent the others from being processed.

**Output:** JSON summary object.
```json
{
  "total": 3,
  "succeeded": 2,
  "failed": ["2"]
}
```

**Exit codes:**
- `0`: success (all IDs marked as read)
- `1`: configuration error
- `2`: runtime error (one or more IDs not found/invalid, or authentication failed)

---

## Error Handling

- **Missing configuration file:** Show clear message indicating where `appsettings.json` or `agent-tooling.json` should be placed.
- **Server not found:** Error message lists available servers.
- **Authentication failure:** Error message includes server name and username attempted.
- **Connection error:** Error message includes server address and port.
- **Email ID not found:** Error message indicates invalid ID.

## Design Constraints

- No CLI secrets: all credentials in configuration files.
- No attachment handling: emails with attachments are processed but attachments are ignored.
- Minimal output: JSON format optimized for token consumption by LLMs.
- HTML to text conversion: uses HtmlAgilityPack to strip all HTML tags, preserving text content only.
