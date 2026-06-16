# docs

## Nav
| path | content |
|------|---------|
| `design/` | UI/UX specs, wireframes, game design documents | → `design/AGENTS.md` |
| `decisions/` | Architecture Decision Records (ADRs) — one file per decision | → `decisions/AGENTS.md` |
| `release_notes/` | Version history and store listing notes | → `release_notes/AGENTS.md` |
| `refs/` | Platform dependency docs (platform-auth contracts, infra guide) | → `refs/AGENTS.md` |

## Rules
- `decisions/` — NEVER delete ADR files; mark superseded ones as `Status: superseded-by ADR-XXX`
- ADR filename format: `ADR-NNN-kebab-case-title.md`

## ADR Format
```markdown
# ADR-NNN: [Title]
Date: YYYY-MM-DD
Status: accepted | superseded-by ADR-XXX

## Context
[Why this decision was needed]

## Decision
[What was decided]

## Consequences
[Tradeoffs, good/bad outcomes, follow-up work]
```
