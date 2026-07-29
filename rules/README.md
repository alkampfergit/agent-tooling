# Tool Quality Rules

Each file in this directory is one quality rule that every CLI tool in `src/` must satisfy.
Rules are written to be self-contained: what to check, how to verify it, and the pass
criteria. They are consumed by the `quality-expert` agent (`.claude/agents/quality-expert.md`)
but can be referenced from any skill or agent that needs to assess or implement tool quality.

## Rule format

Every rule file follows this structure:

```markdown
# Rule: <short title>

**Id:** <kebab-case-id, same as filename>
**Applies to:** <which tools>

## Requirement
What the tool must do.

## How to verify
Concrete command(s) to run and what to look for.

## Pass criteria
Objective checklist — all items must hold for the rule to pass.
```

## Adding a rule

Create a new `<rule-id>.md` file following the format above. The `quality-expert`
agent picks up all `*.md` files in this directory automatically (except this README).
