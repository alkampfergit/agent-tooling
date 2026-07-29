---
description: Retrospective on the current conversation and recent session history to improve the harness configuration — CLAUDE.md, skills, agents, rules, hooks, memory. Use when the user asks to run a retrospective, "learn from this session", improve the project/global config, consolidate memory, or audit skills — even mid-conversation. Arguments add extra weight to specific areas but never limit scope. Do NOT use for improving application code; use /simplify or /review for that.
argument-hint: "[optional focus areas]"
---

# Learn — configuration retrospective

Review the conversation + recent history, cross-reference against every config layer,
present improvement suggestions one at a time via AskUserQuestion, then apply approved
changes. Always do a full sweep; any arguments passed are targeted feedback that gets
highest priority but does not narrow scope.

**Announce:** "Starting retrospective..."

## Load prior learnings

Read `~/.claude/learn-log.md` if it exists (created at the end of each run). Use it to:
deprioritize finding types consistently rejected, boost categories consistently
accepted, and adapt rule-writing style to observed Modify signals (e.g. user repeatedly
softening NEVER → Avoid). Missing file = first run, proceed normally.

## Scope selection

Before launching anything, ask via AskUserQuestion: "What scope should this
retrospective cover?"

- **Historical + current conversation** — history from recent sessions, prior /learn
  audit, plus current conversation analysis
- **Current conversation only** — analyze only this session
- **Config audit only** — no conversation analysis; full config health scan (memory
  consolidation, CLAUDE.md bloat, content placement, skill consistency). Best from a
  fresh conversation.

Store the answer as `scope` for the rest of the run.

## Phases

Run the phases below in order. Detailed procedures live in `references/` — read the
relevant file at the start of each phase.

| Phase | What | Details |
|-------|------|---------|
| 1–2 | Config discovery + history scan + prior-/learn cross-check (background, parallel) | `references/discovery.md` |
| 3 | Current conversation analysis (foreground; skip in config-audit scope) | `references/analysis.md` |
| 4 | Cross-reference, config health checks, categorize + confidence-score findings | `references/analysis.md` |
| 5 | Present findings one at a time via AskUserQuestion | `references/present-apply.md` |
| 6 | Apply approved changes, summary table, save learnings to `~/.claude/learn-log.md` | `references/present-apply.md` |

Scope gates:
- **Current only** → skip history scan and prior-/learn agents; session-based
  confidence caps at Medium unless there's a direct user correction; skip pattern
  promotion (4b) and the prior-runs audit table.
- **Config audit only** → additionally skip Phase 3 entirely.

## Standing rules (apply throughout)

- **Environment fit:** this setup uses tokensave MCP for code exploration — NEVER
  propose or launch Explore agents for codebase research. Config-file discovery uses
  Glob/Read/Grep directly (config markdown is not in the code graph, so this is fine).
- Memory files must follow the harness memory format: frontmatter with `name`,
  `description`, `metadata.type` (user/feedback/project/reference), body with
  **Why:** / **How to apply:** for feedback, `[[links]]`, and a one-line pointer in
  `MEMORY.md`. Never put memory content in MEMORY.md itself.
- **Never claim redundancy without grep verification.** Before flagging any file as
  "already covered elsewhere" or proposing a deletion, grep the claimed destination and
  present the evidence inline. Zero matches → offer keep/bake-into-skill/promote
  instead of delete.
- **Never remove content before its destination exists.** Consolidate-then-clean:
  integrate first, confirm, then delete the original.
- **Default to recommending.** Every finding leads with a specific recommendation and
  rationale, never an equal-weight menu.
- Proposed rules start critical directives with NEVER/ALWAYS, lead with the why, use
  precise language, include an example when not self-evident, and stay to one sentence
  where possible.
- Never silently proceed with incomplete data — if an agent fails or a check is
  skipped, tell the user what was skipped and why.
- Paths: this is Windows; always use forward slashes in anything you write, and quote
  paths containing spaces in generated hook commands.

## Comprehensive config session mode

When the user explicitly asks for a dedicated sweep ("full config sweep",
"comprehensive improvement", "config audit", "improve everything"): make the memory
audit a dedicated phase (read every memory file, grep-verify each against CLAUDE.md and
skills), use wave-based parallel execution in Phase 6 (group by file, no two agents
edit the same file in a wave, show the wave structure first), and still present every
improvement individually — never batch into an assumed-approval plan. One-at-a-time
presentation has historically achieved far higher acceptance than batch plans.
