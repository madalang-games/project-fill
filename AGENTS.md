# project-fill

## Nav
| path | role |
|------|------|
| `shared/` | Shared C# contracts, shared types, game meta data | → `shared/AGENTS.md` |
| `tools/` | Automation pipeline (gen-data, gen-packets, gen-orm) | → `tools/AGENTS.md` |
| `client/` | Unity 6 game client | → `client/AGENTS.md` |
| `server/` | ASP.NET Core 8 server + DB schema | → `server/AGENTS.md` |
| `docs/` | Design, technical, decisions, tests, platform refs | → `docs/AGENTS.md` |
| `TODO-List/` | Release tracker, per-area task lists | → `TODO-List/AGENTS.md` |
| `docker-compose.dev.bat` | Starts local dev Docker Compose stack | |

## Pipeline
```
shared/datas/**/*.csv  -> info_generator -> {client,server}/generated/data/**/*.csv
server/db/schema.json  -> db_generator   -> DB CREATE/ALTER TABLE (+ migration SQL)
shared/contracts/*.cs  -> pkt_generator  -> client/Assets/Scripts/Generated/Contracts/
```
CMD: `tools/all_generator.bat` | `tools/info_generator.bat` | `tools/db_generator.bat` | `npm run gen:all`

## Rules
- **AGENTS.md is the Source of Truth (SoT) for AI context.** `CLAUDE.md` and `GEMINI.md` must point to it via `@AGENTS.md`.
- **NEVER edit `*/generated/*`** — edit source (CSV, schema, contracts), re-run the appropriate generator.
- NEVER commit `.env` — use `.env.example`
- NEVER store secrets in `template.ini` — secrets go in `.env`
- CONFIG policy: env vars own deploy/runtime values; `template.ini` owns tooling values; no hardcoded config fallbacks
- `_` prefix files/dirs are skipped by all gen tools (examples, drafts)
- **AGENTS.md Maintenance**: Always update related `AGENTS.md` files (Nav, Symbols, etc.) immediately after completing any task or implementation.
- **Git Commit Protocol**: If `read_file` is blocked by ignore patterns, you MUST use `run_shell_command` (e.g., `Get-Content .claude/issues.cache.md`) to retrieve issue numbers as specified in `.claude/commands/git-commit.md`.

## Clarification Protocol
Stop and ask **before** implementing when: requirement is ambiguous with design impact, a clearly better alternative exists (not just style), or task touches DB schema / auth / cross-service contracts.
Format: `QUESTION: [what] | OPTIONS: A) … B) … | RECOMMEND: [A/B] — [reason]`
Don't ask: clear best practice, cosmetic difference, same outcome different syntax.
Small improvement spotted → implement as requested + append `NOTE: [alternative] — ask to switch`.

## Doc Convention
Each content dir needs Doc Set: `AGENTS.md` (AI, English, token-efficient) + `CLAUDE.md`/`GEMINI.md` (exactly `@AGENTS.md`).
- Leaf AGENTS.md: `## Files` (file→class→role) + `## Symbols` (ClassName.MemberName→kind→note) + `## Rules`
- Nav AGENTS.md: `## Nav` (path→role→link) + minimal `## Rules`
- New files/logic: update Files/Symbols; new subdir: create Doc Set + update parent Nav
- Cross-refs in leaf/source AGENTS.md: `Consumed by:` / `Depends on:` / `Gen output:` — use `Layer.ClassName`

## New System Checklist
When adding a cross-cutting system (touches ≥2 of: data / server / client):
1. `shared/datas/[domain]/` — define CSV schema → update AGENTS.md
2. `shared/contracts/` — define request/response DTOs → update contracts AGENTS.md
3. `server/db/schema.json` — add table definition → run `gen:orm`
4. Server layers (Domain → Infrastructure → API) — implement → update each AGENTS.md
5. Client — implement → update AGENTS.md
6. Run `tools/all_generator.bat`
7. Update `TODO-List/AGENTS.md` progress

## Search

**Decision order — stop at first match:**
1. Path in loaded AGENTS.md `## Nav` or `## Files` → use that path directly with Glob/Grep
2. Symbol needed, path known → `rg "Symbol" path/to/dir --type cs`
3. Path unknown, scope ≤2 dirs → `Get-ChildItem` or targeted Glob
4. Scope unknown OR cross-cutting (≥3 dirs, unfamiliar area) → spawn `Explore` subagent

**Never spawn Explore when:** path is already in loaded AGENTS.md context.

| goal | tool |
|------|------|
| file location (path in nav) | Glob with exact path+extension |
| symbol definition | `rg "ClassName" --type cs -l` |
| all implementors of interface | `rg "IInterface" --type cs -l` |
| role / ownership | read that dir's `AGENTS.md` |
| structure of unfamiliar/unknown area | Explore subagent |

**Glob rules:**
- Always scope to specific path + extension: `client/project-fill/Assets/**/*.cs`
- Never `client/**/*` — pulls Unity Library/PackageCache noise

## Output
- No narration before tool calls — execute immediately
- Silent on success path — only surface errors or blockers
- Final report: compact table or key-value pairs, no prose
