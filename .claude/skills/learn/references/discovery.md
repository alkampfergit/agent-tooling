# Phases 1–2 — Discovery, history scan, prior-/learn cross-check

Launch depends on scope:

- **Historical + current** → launch all three tasks in parallel (history scan and
  prior-/learn cross-check as background general-purpose agents), then proceed
  immediately to Phase 3.
- **Current only** → run only Config Discovery, then Phase 3. Announce: "Running
  discovery (current conversation scope)."
- **Config audit only** → run only Config Discovery, then skip straight to Phase 4.
  Announce: "Running config-only audit — skipping conversation analysis."

## Config discovery (always runs — foreground, direct tools)

Do NOT use an Explore agent (see standing rules). Use Glob/Read directly to catalog
ALL config-like files at BOTH project and global levels:

- CLAUDE.md / AGENTS.md — project root, project `.claude/`, and global `~/.claude/CLAUDE.md`
- `.claude/commands/` and `.claude/skills/` — project AND global `~/.claude/skills/`, `~/.claude/commands/`
- `.claude/agents/` — project and global
- `.claude/rules/` — project, and global `~/.claude/rules/`
- Memory: `~/.claude/projects/<project-path>/memory/` (project) and the global-level
  memory directory (typically `~/.claude/projects/-<home-dir>/memory/`)
- Settings: project `.claude/settings.json`, `.claude/settings.local.json`, global
  `~/.claude/settings.json`, `~/.claude/settings.local.json`
- Shared frameworks/guardrails/style guides (`shared/`, `frameworks/`), voice/brand
  files, and any other instruction-like `.md` governing behavior

Produce a **config map**: files with purpose, organized by type AND level (project vs
global).

Fallback if discovery hits errors: hardcoded scan of the known paths above, and
announce "Discovery failed — using fallback config path scan."

## History scan agent (general-purpose, background — full scope only)

Prompt the agent to:

1. Write a bash script that:
   - Lists `.jsonl` session files from `~/.claude/projects/<project-path>/`
   - Sorts by modification date, takes the 5 most recent (excluding the current session)
   - Extracts ONLY user-message lines (type "human") — skip assistant responses and
     tool calls
   - Filters for feedback signals:
     - Corrections: "no", "don't", "stop", "not that", "wrong", "actually", "instead"
     - Praise: "yes", "perfect", "exactly", "great", "love", "nice"
     - Explicit feedback: "improve", "better", "should", "could you", "I wish", "next time"
     - Frustration: repeated requests, "again", "I already said"
   - Saves extracted messages with session date to a scratchpad temp file; no cap on signals
2. Read the temp file and organize findings: tag each with session date + brief context
   quote; group by type (corrections, praise, friction, capability gaps); note
   recurring patterns across sessions (same feedback 2+ times = promotion candidate).

Return categorized findings with source citations — concise summaries, not raw data.

If it fails: gracefully degrade to current-conversation-only scope, announce "History
scan returned no results — skipping cross-session analysis," and skip pattern
promotion and the prior-runs audit table.

## Prior-/learn cross-check agent (general-purpose, background — full scope only)

Audits what prior `/learn` runs recommended and whether accepted changes landed.

For each session file from the history scan, check whether `/learn` was invoked
(`grep -l "/learn"` on the .jsonl). For every such session:

1. **Extract the "Changes Applied" summary table** (printed in Phase 6), or if absent,
   parse AskUserQuestion responses for Accept/Reject/Modify decisions. Capture:
   recommendation text, target file, decision.
2. **For each Accepted recommendation**, verify it landed: read the target file, grep
   for the key phrase that was supposed to be added. Mark **Verified Implemented**
   (present), **Drifted** (file exists, text missing/modified), or **Missing** (target
   file doesn't exist).
3. **For each Skipped/Rejected recommendation**, flag for re-surfacing: original date +
   session ID, what was recommended and why declined (if captured), and whether the
   underlying friction has recurred since (cross-reference current history-scan
   signals).

Return a structured report under 400 words, citing session dates and target files:
prior runs (dates), verified implemented (audit trail only), accepted-but-drifted/
missing (needs re-application), previously-skipped-but-still-signaling (re-surface as
current findings).
