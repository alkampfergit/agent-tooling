# Phases 3–4 — Conversation analysis, cross-reference, categorization

## Table of contents

- Phase 3: Current conversation analysis
- Phase 4a: Enforcement gap detection (rules → hooks)
- Phase 4b: Progressive evolution (pattern promotion)
- Phase 4c: Config health & consolidation (size, memory, placement, skills, budget)
- Phase 4d: Categorize findings, confidence scoring, placement recommendation

## Phase 3 — Current conversation analysis (foreground)

Skip entirely in config-audit scope. Announce: "Analyzing current conversation for
patterns, feedback, and techniques..."

Analyze the conversation already in context for:

| Signal | What to look for |
|--------|------------------|
| Corrections | User corrected behavior, said "no", "don't", "stop", asked to redo |
| Praise | User confirmed approach, accepted without pushback |
| Friction | Multiple attempts, confusion, back-and-forth |
| Capability gaps | User did things manually, asked for something Claude couldn't do |
| Behavioral patterns | Tone issues, over/under-explaining, wrong assumptions |
| Targeted feedback | Arguments passed to /learn — HIGHEST PRIORITY |
| Repeated workflows | Multi-step manual processes that could become a skill |
| Techniques discovered | Novel approaches that worked — new methods, clever tool usage |
| User interaction patterns | Prompting styles that led to better/worse results |

Low signal → say "No significant findings from this session" and proceed with
history/config findings.

## Phase 4 — Cross-reference & categorize

Announce: "Cross-referencing findings against config files..." Wait for background
agents, validate each result (see failure handling in `discovery.md`), then read each
config file from the config map.

### 4a — Enforcement gap detection

For each existing rule, check whether the conversation shows it being violated.

- Violated once → suggest strengthening (emphasis, position, examples), not removal.
- Repeated violation (across sessions in full scope, or multiple times in-session in
  current-only) → suggest **converting to a hook**: hooks are deterministic, CLAUDE.md
  instructions are probabilistic (~80% compliance).

When suggesting a hook, generate the complete implementation: pick the event
(`PreToolUse` + matcher for command/tool gating, `PostToolUse` for validation,
`Notification` for reminders), produce ready-to-paste JSON for settings.json, and
include the enforcing shell command. Quote script paths that contain spaces. Note:
"Advisory today (~80% compliance); as a hook it becomes deterministic."

```json
{
  "hooks": {
    "PreToolUse": [
      { "matcher": "Bash", "hooks": [ { "type": "command", "command": "..." } ] }
    ]
  }
}
```

Present enforcement gaps with three options: **Strengthen rule** (NEVER/ALWAYS
emphasis, move to top), **Convert to hook** (include the generated JSON; then ask a
second AskUserQuestion for hook scope: project settings.json / global / project
local), or **Both**.

### 4b — Progressive evolution (pattern promotion) — full scope only

When history shows the same feedback across 2+ sessions, suggest promoting: memory →
CLAUDE.md rule; buried rule → top of CLAUDE.md with NEVER/ALWAYS; implicit pattern →
explicit rule with examples; soft guideline → hard rule.

**Promotion cleanup:** a memory → CLAUDE.md promotion ALWAYS pairs with deleting the
original memory file (and its MEMORY.md index line), presented as one finding.

### 4c — Config health & consolidation

Run every sub-check against BOTH project and global configs.

**Size thresholds.** CLAUDE.md: Warning >150 lines or >20K chars; Critical >200 lines
or >40K chars (Anthropic recommends under ~200 lines; compliance decays with
instruction count). MEMORY.md: Warning >120 lines, Critical >160. Secondary: >50 memory
files in one project = flag, >70 = Critical.

**Memory consolidation.** Group memory files by topic; flag duplicates/heavy overlap
(recommend merging), stale references (files/functions that no longer exist), and
relative dates never converted to absolute.

**Promoted-but-not-cleaned.** For each feedback memory, grep CLAUDE.md for the key
phrase of its core rule. Same rule, same scope/intent → flag as redundant with grep
evidence ("promoted to CLAUDE.md line N, original never cleaned up") and recommend
deleting the memory (CLAUDE.md is authoritative).

**Stale project memory.** For each project-type memory: does it reference a milestone
2+ versions behind current (check git log)? Does it describe a completed one-time event
(audit result, retest, deployment verification) vs an ongoing decision? Completed
events → "stale — recommend deletion"; ongoing decisions → keep or merge.

**Mandatory redundancy verification.** Before flagging ANY file as redundant: grep the
target for the key phrase, confirm the match covers the same scope and intent, present
the evidence ("Verified: [target] line N contains [text]"). NEVER claim redundancy
without it.

**Consolidate-then-clean.** Decide where each rule should live (skill / CLAUDE.md /
memory) using the placement logic below; integrate there FIRST; only after integration
is confirmed, remove the original. Never delete content whose destination doesn't yet
hold it.

**Rule extraction.** CLAUDE.md instructions scoped to file types or paths ("for
*.test.ts", "in API routes") → migrate to `.claude/rules/` with path-scoping globs in
frontmatter, so they only load when matching files are touched.

**Skill extraction.** CLAUDE.md sections >~20 lines that read like procedures /
multi-step workflows / decision trees → convert to skills (on-demand loading, ~100
tokens of metadata vs always-on content).

**Skill consolidation.** Across project and global skill/command dirs, flag:
overlapping skills (mergeable), oversized skills (grown past their purpose), stale
skills (referencing things that no longer exist), shadowed skills (project skill with
the same name as a global one — intentional override or accident?).

**Cross-skill consistency.** Review all skills holistically for: conflicting
directives (ALWAYS X vs NEVER X), overlapping trigger conditions, inconsistent
terminology for the same concept, process conflicts for the same scenario, and skills
contradicting CLAUDE.md without acknowledgment. Present contradictions as Critical
with both sources cited (paths + lines).

**Quality-standards distribution.** Classify each CLAUDE.md quality rule as
*universal* (keep), *brainstorming-relevant* (guides design/investigation before a
skill loads — keep but shorten to one line if verbose), or *skill-specific* (only
applies during one skill's execution — move into that skill, or remove from CLAUDE.md
if already there).

**Content placement audit — five directions.**
1. CLAUDE.md → skill: sections referencing a specific skill by name, or procedures
   only relevant during one workflow type (brainstorming, reviewing, debugging).
2. Memory → skill: feedback/project memories containing multi-step procedures or
   decision trees; feedback specific to ONE skill's execution → bake into that skill
   file and delete the memory.
3. Skill → CLAUDE.md: ONLY universal behavioral rules found inside a skill (rules
   about the skill's own procedure stay put).
4. CLAUDE.md → memory: factual/reference content that isn't a behavioral instruction.
5. Skill ↔ skill: near-identical copy-pasted sections → extract to a shared reference.

**Skill budget monitoring.** Sum characters across ALL skill `description` fields
(both levels). Warning >12K chars, Elevated >15K. Skills over budget are silently
dropped from the system prompt; suggest `/doctor` or `/context` to verify visibility,
and `"skillListingBudgetFraction": 0.03` in settings.json if dropping occurs. Always
Maintenance-tier (budget mechanics are unofficial and may change) unless the user
reports actually losing skills.

**Skill description quality.** For each skill: third person? trigger conditions ("Use
when...")? 130–263 chars? concrete keywords, not vague? Activation rates range ~20%
(bad description) to ~90% (optimized). Maintenance-tier, with suggested rewrites.

**CLAUDE.md structure.** Light-touch check that sections follow WHAT (context/stack)
/ WHY (principles/anti-patterns) / HOW (workflows/commands); flag only genuinely
unclear mixing, as Maintenance-tier.

**Cross-level analysis.** Duplicated rules between project and global CLAUDE.md;
contradictory instructions across levels; memory filed at the wrong level; skills
existing at both levels with different content.

### 4d — Categorize findings

| Category | When | Priority |
|----------|------|----------|
| Targeted | From explicit /learn args | 1st |
| Critical | Caused errors, repeated correction, enforcement gaps | 2nd |
| Promotion | Recurring cross-session pattern needing a stronger rule | 3rd |
| Content Misplacement | Content living in the wrong config layer | 4th |
| Improvement | Enhancement to existing rules/skills/behaviors | 5th |
| Technique | Novel approach that worked — document for reuse | 6th |
| Maintenance | Bloat, contradictions, staleness, budget, descriptions | 7th |
| Reinforcement | Worked well — strengthen existing documentation | 8th |
| New Skill | Repeated pattern that could become a dedicated skill | 9th |
| User Coaching | Gentle suggestion for better user-AI interaction | Last |

Skip findings already documented AND being followed.

**Confidence scoring** (secondary axis; doesn't change priority order, guides user
scrutiny): **High** = 3+ signals, or recurrence across 2+ sessions, or direct user
correction. **Medium** = 1–2 signals from the current session, or pattern match
without direct evidence. **Low** = speculative, inferred from structure/best
practices. In current-only scope, session signals cap at Medium unless there's a
direct correction.

**Placement recommendation** (attach to every finding): procedural rule tied to one
skill → that skill file; applies across 2+ skills but not universal → CLAUDE.md
quality standards; fact or reference → memory file. ALWAYS recommend one specific
placement with rationale — never equal-weight options.
