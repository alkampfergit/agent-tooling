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
        "Type": "Imap",
        "Default": true,
        "Address": "imap.example.com",
        "Port": 993,
        "Username": "user@example.com",
        "Password": "secret",
        "UseHttps": true
      },
      {
        "Name": "o365",
        "Type": "Office365",
        "Username": "user@contoso.com",
        "ClientId": "<entra-app-client-id>",
        "TenantId": "common"
      }
    ]
  }
}
```

Each server entry has a `Type` field: `"Imap"` (default when omitted, preserving existing configs) or `"Office365"`. Field requirements are validated per type at runtime (not via compile-time `required`):
- `Imap`: `Address`, `Port`, `Username`, `Password`, `UseHttps` are required.
- `Office365`: `Username` (the mailbox UPN/email to sign in as), `ClientId` (Entra app registration's Application ID) are required. `TenantId` is optional, defaults to `"common"`. `Address`, `Port`, `Password`, `UseHttps` are not used and must not be set.

Each server entry also has an optional `Default` boolean field (defaults to `false`/omitted). It controls which server is used when `--servername` is omitted:
- If only one server is configured, `--servername` is optional and that server is used regardless of `Default`.
- If multiple servers are configured and `--servername` is omitted:
  - If one or more servers have `Default: true`, the **first** one found (in config array order) is used.
  - If none are marked `Default: true`, the command fails with an error listing the available server names.

## Office 365 / Microsoft Graph Support

For servers with `Type: "Office365"`, `summary`, `get-details`, and `mark-read` operate against the user's mailbox via the Microsoft Graph API (`Microsoft.Graph` SDK) instead of IMAP. No new commands are added — the existing three commands transparently support both server types.

### Authentication

- Uses the OAuth2 device-code flow for a **public client** Entra app registration (`ClientId` + `TenantId`, no client secret — public clients cannot hold one).
- Requests the delegated `Mail.ReadWrite` scope (covers read and mark-read operations).
- On first use (no cached token), the tool prints a verification URL and one-time code to stderr/console and blocks until the user completes sign-in in a browser (on any device). It never launches a browser itself.
- The resulting token (including refresh token) is persisted via `Microsoft.Identity.Client` (MSAL.NET) + `Microsoft.Identity.Client.Extensions.Msal`, using OS-native secure storage:
  - **Windows:** DPAPI-encrypted file, scoped to the current Windows user.
  - **macOS:** Keychain.
  - **Linux:** libsecret ("Secret Service" — gnome-keyring or kwallet).
- On Linux, if no Secret Service is available, the tool does **not** fall back to a plaintext cache. It fails with a clear configuration error instructing the user to install/start gnome-keyring or kwallet, and requires a fresh device-code login on every invocation until then.
- Subsequent invocations silently redeem a cached/refreshed access token — no prompt unless the refresh token has expired or been revoked.
- Alongside the encrypted MSAL token cache, an `AuthenticationRecord` is serialized to `%LOCALAPPDATA%/AgentTooling/Smtp/{server-name}.authrecord.json` (or the platform equivalent) after the first sign-in. This is required by `DeviceCodeCredential` to identify which cached account to use for silent token acquisition on later runs — without it, the credential cannot locate the right account in the persistent cache and re-prompts every invocation.

### Data mapping

- Graph message IDs (opaque strings) are used directly as the `id` field in `EmailSummary`/`EmailDetails`/`MarkReadSummary` — no format changes to existing models.
- `summary` maps from `GET /me/mailFolders/inbox/messages?$filter=isRead eq false&$select=id,from,subject,receivedDateTime,bodyPreview&$orderby=receivedDateTime desc`, truncating `bodyPreview` to 200 characters for the `preview` field (consistent with the IMAP path).
- `get-details` maps from `GET /me/messages/{id}` (`from`, `toRecipients`, `ccRecipients`, `subject`, `receivedDateTime`, `body`). HTML bodies are still routed through the existing `HtmlToTextConverter` for parity with the IMAP path's "strip all HTML" behavior.
- `mark-read` issues `PATCH /me/messages/{id}` with `{"isRead": true}` per ID, same best-effort-per-ID behavior as the IMAP path.

### Design Constraints (Office 365)

- No attachment handling, consistent with the IMAP path.
- No new CLI commands or options — `Type` in configuration is the only new surface area.
- No client secret in configuration — device-code flow only supports public client apps.

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
- **Missing Office 365 config fields:** Error message names the missing field (`ClientId` or `Username`) and the server name.
- **No Secret Service on Linux:** Error message instructs the user to install/start gnome-keyring or kwallet; does not fall back to plaintext storage.
- **Device-code sign-in required:** Verification URL and code are printed to stderr; the command blocks until sign-in completes or the code expires (per Entra's device-code expiry, typically 15 minutes).

## Design Constraints

- No CLI secrets: all credentials in configuration files.
- No attachment handling: emails with attachments are processed but attachments are ignored.
- Minimal output: JSON format optimized for token consumption by LLMs.
- HTML to text conversion: uses HtmlAgilityPack to strip all HTML tags, preserving text content only.
