# Search Tool

A minimal CLI tool for searching the web. Returns ranked result links, each with a short
summary, as JSON on stdout. Optimized for low token consumption with LLMs.

Only DuckDuckGo is implemented, and it needs no API key or account — the tool works out
of the box with no configuration.

## Quick Start

### View Help
```bash
search --help
```

### Usage

```bash
search "netmq performance"                      # 10 results, default engine
search "netmq performance" --max-results 3      # Fewer results
search "netmq" --engine duckduckgo              # Explicit engine
search "netmq" --timeout 10                     # Shorter HTTP timeout
```

### Options

| Option          | Default      | Description                                        |
|-----------------|--------------|----------------------------------------------------|
| `<query>`       | *(required)* | Search terms. Quote the value to include spaces.   |
| `--engine`      | configured   | Engine to use. Supported: `duckduckgo`.            |
| `--max-results` | `10`         | Maximum results to return (1–50).                  |
| `--timeout`     | `30`         | HTTP timeout per request, in seconds (1–300).      |

### Output

```json
{
  "query": "netmq performance",
  "engine": "duckduckgo",
  "abstract": {
    "text": "ZeroMQ is an asynchronous messaging library...",
    "source": "Wikipedia",
    "url": "https://en.wikipedia.org/wiki/ZeroMQ"
  },
  "results": [
    {
      "rank": 1,
      "title": "GitHub - zeromq/netmq",
      "url": "https://github.com/zeromq/netmq",
      "snippet": "NetMQ is a 100% native C# port of the lightweight messaging library ZeroMQ..."
    }
  ]
}
```

`abstract` is a single blurb about the **whole query**, not a per-result summary. It comes
from DuckDuckGo's Instant Answer API and is `null` for most non-encyclopedic queries.
Each result carries its own `snippet`, which is the summary for that page.

A search that matches nothing returns `"results": []` and exits `0`.

## Configuration

None is required. To list several engines and pick which one `--engine` defaults to,
create `appsettings.json` in your working directory (or `agent-tooling.json` in a parent
directory, shared across all tools):

```json
{
  "Search": {
    "Engines": [
      { "Name": "duckduckgo", "Default": true }
    ]
  }
}
```

- `Name` — engine identifier, matched case-insensitively.
- `Default` — when `true`, this engine is used if `--engine` is omitted.

Listing an engine does not implement it. Only `duckduckgo` is supported by this build;
selecting any other configured name is an error. With no configuration, the tool behaves
as if `duckduckgo` were configured as the default.

If several engines are configured and none is marked `Default`, `--engine` becomes
mandatory. Marking more than one as `Default` is an error.

## Exit Codes

| Code | Meaning                                                                        |
|------|--------------------------------------------------------------------------------|
| `0`  | Success — including a search that returned zero results.                        |
| `1`  | Configuration or argument error (unknown engine, out-of-range option, no query).|
| `2`  | Runtime error (HTTP failure, rate limiting, timeout, unparseable response).      |

Errors go to stderr; stdout carries JSON only.

## Caveats

DuckDuckGo has no official API that returns ranked web links. The Instant Answer API used
for `abstract` is official and stable, but the result links come from the unofficial
`html.duckduckgo.com` endpoint. That endpoint is unauthenticated, rate limited, and may
change without notice.

When it decides a caller looks automated it serves a bot-verification challenge **with
HTTP status 200**, not 403 or 429. The tool detects this and fails with exit code 2 rather
than silently reporting zero results. If you hit it, slow down and retry later.
