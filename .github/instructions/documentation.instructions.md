---
applyTo: 'docs/**/*.md'
description: 'Document type standards, structure templates, and conventions for GiviKDev.OAuth documentation'
---

# Documentation Standards

GiviKDev.OAuth documentation lives in `docs/` organized
by type. Each document type has a defined goal, structure,
and conventions.

## Status Markers

Use blockquote markers inside docs to distinguish
current state from planned state:

- `> **Target state.**` — describes something not yet
  built. Include a brief note on what exists today if
  relevant.
- `> **Verified against {version}.**` — protocol
  finding confirmed against a specific SDK or IdP
  version.
- `> **Stale.**` — finding or design that may no
  longer apply. Needs re-verification.

## Document Types

### Foundation (`docs/01-foundation/`)

**Goal:** Project identity — problem, vision, scope,
domain concepts, solution architecture, roadmap,
protocol findings.

**Structure:** Narrative. `# Title`, brief intro,
then sections as needed. No rigid template.

**Rules:**
- Mark unbuilt capabilities with `> **Target state.**`
- Scope doc (`003-scope.md`) is the authority on what
  is in/out of scope
- Roadmap (`006-roadmap.md`) is the authority on what
  is built vs planned
- Protocol findings (`007-protocol-findings.md`)
  document the behaviours that justify design choices.
  Each finding must include the consequence and how to
  verify it.

### Decisions (`docs/02-decisions/`)

**Goal:** Record a specific architectural or
technology decision.

**Structure:**

```
# Title

## Status

Accepted | Superseded by [XXX]

## Context

Why this decision was needed.

## Decision

What we decided. Be specific.

## Consequences

Positive and negative outcomes.
```

**Rules:**
- ADRs are immutable after acceptance
- To change a decision, create a new ADR that
  supersedes the old one

### Spikes (`docs/03-spikes/`)

**Goal:** Exploration and analysis of technologies,
approaches, or patterns. Informs future decisions.

**Structure:**

```
# Title

## Context

What question we are trying to answer.

## Recommendation

What approach we recommend and why.

## Analysis

Detailed comparison, benchmarks, trade-offs.

## References

External sources consulted.
```

**Rules:**
- Use "Recommendation" not "Decision" — spikes
  inform decisions but are not themselves decisions
- These are immutable records of exploration

## When Creating or Editing Docs

1. Identify the document type from the folder
2. Follow the structure template for that type
3. Apply status markers where needed
4. Link to related docs rather than duplicating
   content
5. Keep docs alive — update when reality changes
