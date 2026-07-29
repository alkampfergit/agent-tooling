# Phases 5–6 — Present findings, apply changes, save learnings

## Table of contents

- Phase 5: Presentation order, formats, memory-deletion gate
- Phase 6: Applying changes (hooks, extractions, merges, moves), summary table
- Save learnings to ~/.claude/learn-log.md

## Phase 5 — Present findings

Announce: "Found N findings across M categories. Presenting one at a time, most
impactful first."

### Prior-runs audit table (full scope only — show FIRST)

Before any new findings, surface the prior-/learn cross-check as an audit trail:

```
## Audit of Prior /learn Runs

| Date | Recommendations | Implemented | Drifted | Skipped |
|------|----------------|-------------|---------|---------|
| 2026-04-11 | 6 | 6 OK | 0 | 0 |
| 2026-04-09 | 5 | 3 OK | 1 !  | 1 (re-surfaced below) |
```

Verified-implemented builds confidence; drift needs re-application; previously-skipped
items re-surface as findings. NEVER silently drop prior findings.

### Presenting each finding (AskUserQuestion, one at a time)

**Question format:** "[Tier | Confidence] — [Source: current conversation / session
date] — [finding + proposed change]. File: [path]. Proposed: [what to
add/modify/remove]" — **Options:** Accept / Reject / Modify.

**Order:** 1. drifted items (prior accept didn't land) → 2. re-surfaced
previously-skipped ("previously skipped on [date]") → 3. Targeted → 4. Critical →
Promotion → Content Misplacement → Improvement → Technique → Maintenance →
Reinforcement → New Skill → User Coaching.

If 8+ findings, after 5 ask: "Continue with remaining findings, or apply what we have
so far?"

### Memory consolidation findings

Present consolidations during Phase 5 (never defer the decision to Phase 6): which
files are redundant, WHERE each rule integrates, what gets removed — approved
per-group.

**BLOCKING gate:** before presenting ANY delete-as-redundant finding, run batch grep
verification on ALL proposed deletions. Present with evidence inline: "Verified:
[file] line N contains [text]." Zero matches → present with placement options
(keep / bake into skill / promote) instead of delete.

**Memory finding format:** "Recommendation: [delete (redundant, verified: {grep
evidence}) / keep as memory / bake into {skill} / promote to CLAUDE.md] —
[rationale]." Options mirror the recommended placements, not generic Accept/Reject.

## Phase 6 — Apply changes

Announce: "Applying N approved changes across M files..."

**Scaling:** <10 changes → sequential. 10+ → group by target file and execute in
parallel waves via sub-agents (no two agents edit the same file in a wave); show the
wave structure first.

**Verify-before-removing gate:** before ANY removal, re-grep the destination to
confirm the content actually exists there. Grep fails → skip the removal and flag:
"SKIPPED: [rule] approved for removal but grep shows it's not in [target]. Keeping
original."

1. Group approved changes by file; edit existing files; create new files as needed.
2. Hook conversions: read the chosen settings file, add the hook under the right event
   key (create `hooks` if absent), APPEND — never replace existing hooks.
3. Rule extractions: write to `.claude/rules/<name>.md` with a path-scoping glob in
   frontmatter; remove the section from CLAUDE.md.
4. Memory merges: combine content, update frontmatter to the merged scope, delete the
   duplicate, update the MEMORY.md index.
5. Skill extractions: new skill file with proper frontmatter; move the procedure out
   of CLAUDE.md; leave a one-line "See /skill-name" reference.
6. Feedback-type findings ALSO become memory files in the project memory directory,
   using the harness format: frontmatter `name` / `description` /
   `metadata.type: feedback`, body = rule + **Why:** + **How to apply:**; add the
   MEMORY.md index line.
7. Content moves: remove from source, add to destination's most relevant section,
   adjust formatting to match, preserve meaning.
8. Description rewrites: edit the frontmatter `description` only; preserve intent,
   improve clarity and activation keywords.
9. Print the summary table:

   ```
   ## Changes Applied

   | # | File | Change | Category |
   |---|------|--------|----------|
   ```

   File = short relative path; Change = concise action ("Added rule: …",
   "Hook added: …", "Memory merged: …"); Category = tier from 4d. Close with
   "N changes across M files."
10. Ask if the user wants to commit the changes.

## Save learnings (always, even with zero changes applied)

Update `~/.claude/learn-log.md`:

1. Read it (create on first run with the structure below).
2. Append under `## Recent Runs`: date, acceptance rate by category ("Critical: 3/3,
   User Coaching: 0/2"), Modify choices revealing preferences ("softened NEVER→SHOULD
   for style rules"), detected patterns ("prefers hooks over rule strengthening").
3. If the file exceeds 80 lines: summarize the oldest raw entries into `## Patterns`
   at the top ("3 runs rejected User Coaching → deprioritize"), delete those entries.

```
# Learn Log

## Patterns (summarized from older runs)

## Recent Runs
### YYYY-MM-DD
- Acceptance: Critical 3/3, Improvement 2/4, User Coaching 0/1
- Modify signal: user changed "NEVER" to "Avoid" in a style rule
```
