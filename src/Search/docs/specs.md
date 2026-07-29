# Search Tool — Specification

## Purpose

Performs a web search and returns ranked result links with a short summary for each,
as JSON on stdout. Designed for AI/LLM workflows: minimal, structured output with no
prose wrapper. The search engine is selectable via `--engine`, backed by a configured
list of engines with one marked as the default. The first implementation supports
DuckDuckGo, which requires no API key.

## Configuration

Configuration is loaded via `Agent.Tools.Common.ConfigurationManager.LoadToolConfiguration("Search")`,
which reads (later sources override earlier):

1. `agent-tooling.json` (searched in parent directories, shared across tools)
2. `appsettings.json` (in the current working directory, tool-specific)

Root key `Search`. The tool works with no configuration at all — the section only
becomes necessary once there is more than one engine to choose between:

```json
{
  "Search": {
    "Engines": [
      { "Name": "duckduckgo", "Default": true }
    ]
  }
}
```

`Default` distinguishes engines once several are listed. Only `duckduckgo` is
implemented today, so a second entry is illustrative only — naming an engine here does
not make it available, it only makes it selectable with `--engine`, and selecting an
unimplemented one is an error:

```json
{
  "Search": {
    "Engines": [
      { "Name": "duckduckgo", "Default": true },
      { "Name": "other-engine" }
    ]
  }
}
```

### EngineConfig

| Property  | Type   | Required | Description                                                                 |
|-----------|--------|----------|-----------------------------------------------------------------------------|
| `Name`    | string | yes      | Engine identifier, matched case-insensitively against implemented engines.   |
| `Default` | bool   | no       | When `true`, this engine is used if `--engine` is omitted. Defaults `false`. |

Engines that require credentials will add their own properties when implemented.
No credential fields are defined now.

### Engine resolution

| Situation                                                    | Behavior                                                                                  |
|--------------------------------------------------------------|-------------------------------------------------------------------------------------------|
| `--engine X` matches a configured engine                      | Use that engine.                                                                            |
| `--engine X` not in configuration                             | Exit 1: `Engine 'X' not configured. Configured: <names>`                                   |
| Selected engine is configured but not implemented             | Exit 1: `Engine 'X' is configured but not supported by this build. Supported: duckduckgo`  |
| No `--engine`, exactly one engine has `Default: true`         | Use that engine.                                                                            |
| No `--engine`, no `Default` set, exactly one engine configured | Use that engine.                                                                            |
| No `--engine`, no `Default` set, multiple engines configured   | Exit 1: `Multiple engines configured (<names>). Set "Default": true on one or pass --engine` |
| More than one engine has `Default: true`                      | Exit 1: `Multiple default engines configured (<names>). Only one may set "Default": true`   |
| No configuration file, or `Search.Engines` empty/absent        | Fall back to an implicit `duckduckgo` engine as default — the tool works with zero config.  |

## Command

The root command takes the query directly; there are no subcommands.

**Syntax:**
```
search <query> [--engine <name>] [--max-results <n>] [--timeout <seconds>]
```

**Arguments:**
- `<query>` (required, positional): The search terms. Quote to include spaces.

**Options:**
- `--engine <name>` (optional): Search engine to use. Defaults to the configured default engine.
- `--max-results <n>` (optional): Maximum number of results to return. Default `10`, valid range `1`–`50`.
- `--timeout <seconds>` (optional): HTTP timeout per request. Default `30`, valid range `1`–`300`.

## Behavior

For `duckduckgo`, the engine performs two HTTP calls:

1. **Instant Answer API** — `GET https://api.duckduckgo.com/?q=<query>&format=json&no_html=1&no_redirect=1`.
   If the response has a non-empty `Abstract`/`AbstractText`, it becomes the `abstract`
   field of the output. This is a single, query-level blurb (typically Wikipedia), not a
   per-result summary. Most non-encyclopedic queries return nothing, in which case
   `abstract` is `null`. A failure of this call is non-fatal: `abstract` is `null` and the
   search still succeeds.

2. **HTML results endpoint** — `POST https://html.duckduckgo.com/html/` with the query in
   the form body. The response is parsed with HtmlAgilityPack: each result block yields a
   title and URL from the `result__a` anchor and a summary from the `result__snippet`
   node. DuckDuckGo redirect links of the form `/l/?uddg=<encoded>` are unwrapped to the
   real target URL. Results are truncated client-side to `--max-results`.

A browser-like `User-Agent` header is sent on both calls; DuckDuckGo rejects the default
.NET user agent.

There is no secondary fetch of result pages. Each result's summary is the snippet
DuckDuckGo returns.

## Output

JSON object on stdout:

```json
{
  "query": "netmq performance",
  "engine": "duckduckgo",
  "abstract": {
    "text": "NetMQ is a 100% native C# port of ZeroMQ.",
    "source": "Wikipedia",
    "url": "https://en.wikipedia.org/wiki/NetMQ"
  },
  "results": [
    {
      "rank": 1,
      "title": "NetMQ Documentation",
      "url": "https://netmq.readthedocs.io/",
      "snippet": "NetMQ is a lightweight messaging library..."
    }
  ]
}
```

- `abstract` is `null` when the Instant Answer API returns nothing for the query.
- `results` is `[]` when the search returns no matches. This is a success, not an error.
- Errors are written to stderr; stdout carries JSON only.

## Exit codes

- `0`: success, including a search that returned zero results.
- `1`: configuration or argument error (unknown/unconfigured engine, ambiguous default,
  unimplemented engine, `--max-results` or `--timeout` out of range, empty query).
- `2`: runtime error (HTTP failure, rate limiting / 403, timeout, response parse failure).

## Error handling

- **Empty or whitespace-only query:** exit 1 with a usage hint.
- **Unknown engine:** exit 1, message lists the configured engine names.
- **Unimplemented engine:** exit 1, message lists the engines this build supports.
- **Ambiguous default:** exit 1, message names the candidate engines and how to disambiguate.
- **Rate limited / blocked (HTTP 403 or 429):** exit 2 with a message stating the engine
  rejected the request and that DuckDuckGo's HTML endpoint is unofficial and rate limited.
- **Timeout:** exit 2, message includes the configured timeout.
- **Parse failure (markup changed, no result nodes found where the page was non-empty):**
  exit 2, message states the response could not be parsed.

## Design constraints

- No secrets on the command line: any future engine credentials come from configuration.
- Minimal output: JSON shaped for low token consumption by LLMs.
- Engine implementations sit behind `ISearchEngine` so additional engines are additive.
- The DuckDuckGo HTML endpoint is unofficial, unauthenticated and rate limited; it may
  change without notice. This is an accepted trade-off for a key-free first engine.

## Implementation notes

New project `src/Search/`:

- `Program.cs` — root command wiring.
- `Commands/SearchCommand.cs` — argument/option definitions and handler.
- `Configuration/SearchConfiguration.cs`, `Configuration/EngineConfig.cs`,
  `Configuration/ConfigurationProvider.cs` — binding and engine resolution.
- `Services/ISearchEngine.cs` — `Task<SearchResponse> SearchAsync(string query, int maxResults, CancellationToken ct)`.
- `Services/DuckDuckGoSearchEngine.cs` — HTTP calls and orchestration only.
- `Services/DuckDuckGoHtmlParser.cs` — result extraction from the HTML endpoint. Pure, so
  it can be exercised against captured pages with no network access.
- `Services/DuckDuckGoInstantAnswerParser.cs` — abstract extraction from the API payload.
  Pure and total: never throws, because a missing abstract must not fail a search.
- `Services/SearchEngineFactory.cs` — maps a resolved engine name to an `ISearchEngine`.
- `Models/SearchResponse.cs`, `Models/SearchResult.cs`, `Models/SearchAbstract.cs`.
- `SearchConfigurationException.cs` (exit 1) and `SearchRuntimeException.cs` (exit 2), so
  the command maps exit codes on exception type rather than by matching message text.

`ConfigurationProvider` has a second constructor taking an already-resolved
`IConfiguration` section. The default constructor still goes through
`ConfigurationManager.LoadToolConfiguration("Search")`; the extra one lets engine
resolution be tested without mutating the process working directory.

Bounded integer options (`--max-results`, `--timeout`) parse through
`Agent.Tools.Common.OptionParsers.BoundedInt` so that out-of-range and non-numeric values
are reported identically across tools.

Dependencies: `System.CommandLine`, `HtmlAgilityPack` (both already used in the repo),
`Microsoft.Extensions.Configuration` via `Agent.Tools.Common`.

### Accepted duplication: ConfigurationProvider

`Search/Configuration/ConfigurationProvider.cs` and `Smtp/Configuration/ConfigurationProvider.cs`
share a shape — load section, bind, select a named entry from a list — and the
`common-code-placement` rule flags this. The duplication is deliberate, not an oversight.

The two providers disagree on almost everything the algorithm decides: exception type
(`SearchConfigurationException` vs `InvalidOperationException`), behavior on an empty list
(fall back to `duckduckgo` vs error), name matching (case-insensitive vs case-sensitive),
whether a `Default` flag exists at all, and the wording of all three error messages — each
of which is asserted in that tool's tests. Factoring the shared shape out requires a
selector parameterized on eight axes to unify two ~30-line methods, which reads worse than
either concrete version.

Revisit this if a third tool needs the same selection, or if the two tools' error
semantics converge.

## Testing

Tests live in `src/Agent.Tools.Tests/`, each class carrying `[Category("search")]` plus
`[Category("Unit")]` or `[Category("Integration")]`.

Unit tests (no network — HTML and JSON parsing exercised against captured fixtures):

- DuckDuckGo HTML parsing: titles, URLs and snippets extracted from a representative page.
- Redirect unwrapping: `/l/?uddg=...` resolves to the target URL.
- Empty result page yields an empty result list, not an error.
- Instant Answer JSON with and without an abstract.
- `--max-results` truncation.
- Engine resolution: explicit engine, single configured engine, `Default: true`,
  multiple defaults, multiple engines with no default, unknown engine, unimplemented
  engine, and the zero-config fallback.
- Argument validation: out-of-range `--max-results` and `--timeout`, empty query.

Integration tests (`[Category("Integration")]`, hit the live endpoints):

- A real query returns at least one result with a non-empty title and absolute URL.
- An encyclopedic query populates `abstract`.
