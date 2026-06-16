# project-fill

## Nav
| path | role |
|------|------|
| `shared/` | Shared C# contracts, game meta data | → `shared/AGENTS.md` |
| `tools/` | Automation pipeline (gen-data, gen-packets, gen-orm) | → `tools/AGENTS.md` |
| `client/` | Unity 6 game client | → `client/AGENTS.md` |
| `server/` | ASP.NET Core 8 server + DB schema | → `server/AGENTS.md` |
| `docs/` | Design, decisions, release notes, platform refs | → `docs/AGENTS.md` |
| `TODO-List/` | Release tracker, per-area task lists | → `TODO-List/AGENTS.md` |
| `docker-compose.dev.bat` | Starts local dev Docker Compose stack | |

## Pipeline
```
shared/datas/**/*.csv  -> info_generator -> {client,server}/generated/data/**/*.csv
server/db/schema.json  -> db_generator   -> DB CREATE/ALTER TABLE (+ migration SQL)
shared/contracts/*.cs  -> pkt_generator  -> client/Assets/Scripts/Generated/Contracts/
```
CMD: `tools/all_generator.bat` | `tools/info_generator.bat` | `tools/db_generator.bat` | `tools/pkt_generator.bat` | `npm run gen:all` | incremental: `npm run gen` (`gen:watch`) | verify-only: `npm run gen:check` | cross-source lint: `npm run gen:validate`

## Rules
- **AGENTS.md is the Source of Truth (SoT) for AI context.** `CLAUDE.md` and `GEMINI.md` must point to it via `@AGENTS.md`.
- **NEVER edit `*/generated/*`** — edit source (CSV, schema, contracts), re-run the appropriate generator.
- NEVER commit `.env` — use `.env.example`
- NEVER store secrets in `template.ini` — secrets go in `.env`
- CONFIG policy: env vars own deploy/runtime values; `template.ini` owns tooling values; no hardcoded config fallbacks
- `_` prefix files/dirs are skipped by all gen tools (examples, drafts)
- **No hardcoding**: NEVER use magic numbers or literal strings for branching logic. All discriminating values must be named enum members. Placement: shared (server+client) → `shared/contracts/GameTypes/GameEnums.cs`; server-only → `server/src/ProjectFill.Domain/Enums/`; client-only → dedicated `[Domain]Enums.cs` in the relevant domain folder.
- **AGENTS.md Maintenance**: Always update related `AGENTS.md` files (Nav, Symbols, etc.) immediately after completing any task or implementation.
- **Git Commit Protocol**: If `read_file` is blocked by ignore patterns, you MUST use `run_shell_command` (e.g., `Get-Content .claude/issues.cache.md`) to retrieve issue numbers as specified in `.claude/commands/git-commit.md`.

## Source-of-Truth Map
NEVER edit the right column — edit source (left) and regenerate per **Generator Execution Policy** below.

| Source (edit here) | Generated output — DO NOT EDIT | Generator |
|--------------------|---------------------------------|-----------|
| `shared/datas/**/*.csv` | `client/.../Data/Generated/`, `server/generated/data/` | `tools/info_generator.bat` |
| `server/db/schema.json` | DB SQL, `server/generated/` EF access | `tools/db_generator.bat` |
| `shared/contracts/**/*.cs` | `client/.../Scripts/Generated/Contracts/` | `tools/pkt_generator.bat` |
| `shared/datas/string/client_string.csv` | Font subset assets | `tools/subset_fonts.bat` |

## Generator Execution Policy
Resolves who runs what. Two distinct user-run action classes; do not conflate.

| Action | Who runs | When | Command |
|--------|----------|------|---------|
| **Verify staleness** (read-only, no writes) | **Agent, in-task** | After editing any source, before implementing dependent layers | `npm run gen:check` (or per-gen `gen:info:check` / `gen:db:check` / `gen:pkt:check`) |
| **Write generators** (emit compile-time C#: `info`/`db`/`pkt`) | **Agent**, when build-verification is wanted AND Unity is not required | Before implementing dependent server/client code | `npm run gen:info` / `gen:db` / `gen:pkt` (node, CLI-safe) |
| **Final regen in user env** | **FLAG → user** | Task close | `tools/all_generator.bat` (guarantees `info→db→pkt` order) |
| **DB execute** (`schema.json` → real table) | **FLAG → user** | DB change; default `dry_run=true` writes SQL only | set `dry_run=false`, `tools/db_generator.bat` |
| **Unity Editor menu** (prefab/asset build — NO CLI equivalent) | **FLAG → user** (agent cannot run GUI) | UI structure change via `UIEditorSetup.cs` | e.g. `Unity ▸ Tools/UI Setup/Prefabs/X` |
| **Font subset** | **FLAG → user** | `client_string.csv` change | `tools/subset_fonts.bat` (after `info_generator`) |

Rule: agent runs read-only `gen:check` + CLI write-generators to self-verify; agent FLAGs everything requiring Unity GUI, a real DB, or the user's own env. A `.bat` FLAG and a Unity-menu FLAG are **separate output rows** — never merge them.

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

## Pipeline Protocol
Self-directing execution flow. Apply to every task received as natural language.

### 1. Classify — match ALL that apply
| Signal (keyword / action in task) | Layers to touch |
|-----------------------------------|-----------------|
| game data, CSV, balance, stats, design table | `shared/datas` → FLAG `info_generator.bat` |
| new packet, DTO, API contract, Request/Response type | `shared/contracts` → FLAG `pkt_generator.bat` |
| DB table/schema/column add or modify | `server/db/schema.json` → FLAG `db_generator.bat` |
| server logic, endpoint, business rule | `server/src` + `server/tests` |
| client feature, gameplay, in-game mechanic | `client/.../Scripts` |
| UI **logic/behavior** edit (existing `*View.cs`) | `client/.../Scripts` — direct edit, no regen |
| UI **structure** change (new/changed popup, panel, HUD element) | `UIEditorSetup.cs` → FLAG(Unity) `Tools/UI Setup/Prefabs/X` menu |
| new UI string, text label, localization | `shared/datas/string/client_string.csv` → FLAG `info_generator.bat` (StringIds.cs) **then** `subset_fonts.bat` |
| dynamic image, sprite, atlas | `shared/datas/common/dynamic_resource.csv` |
| spec or design doc reference | `docs/` (read-only) |
| progress update, task completion | `TODO-List/` |

### 2. Execution Order
Dependencies determine order. Parallelize where no dependency exists. Generators that emit compile-time C# run **before** the code that references them — otherwise dependent layers reference symbols that don't exist yet and cannot be build-verified.

1. **[READ]** `docs/` — if task references a spec or design doc
2. **[PARALLEL]** `shared/datas` and `shared/contracts` — mutually independent
3. **[SEQ]** `server/db/schema.json` — after contracts finalized
4. **[GEN]** Agent runs CLI write-generators for changed sources (`gen:info`/`gen:db`/`gen:pkt`) so generated C# exists for the next step. Skip outputs that need Unity GUI or a real DB (those stay FLAG-only; `gen:db` writes EF access + SQL but not the table).
5. **[PARALLEL]** `server/src` and `client/` — both depend on contracts+db, not each other; build-verify against the step-4 generated symbols
6. **[PARALLEL]** `server/tests` and AGENTS.md updates — after implementation
7. **[VERIFY]** Agent runs `npm run gen:check` — confirms no source/output drift remains
8. **[OUTPUT]** FLAG user-run actions (Final Output Format) — `all_generator.bat`, DB execute, Unity menu, `subset_fonts.bat`

### 3. Clarification Gate
Stop and ask (use Clarification Protocol format) ONLY if:
- DB schema / auth / cross-service contract behavior is ambiguous
- Two valid design paths exist with meaningfully different tradeoffs

Otherwise: proceed. Small improvement noticed → implement + append `NOTE: [alternative]` at end.

### 4. Per-Layer Execution
For each layer in DAG order:
1. Load that layer's `AGENTS.md` (Nav → Files → Symbols)
2. Implement
3. Run that layer's **Completion Gate** — output each result explicitly
4. Update that layer's `AGENTS.md`

### 5. Final Output Format
Layer status table, then a separate FLAG block. For multi-generator tasks FLAG `all_generator.bat` (guarantees `info→db→pkt` order) instead of loose individual bats. Unity-menu and DB-execute actions are their own FLAG rows — never folded into a `.bat`.
```
| layer | status |
|-------|--------|
| shared/contracts | ✓ |
| server/src | ✓ |
| client | ✓ |

FLAG (user must run):
- [bat]   tools/all_generator.bat        # multi-gen task → ordered info→db→pkt
- [DB]    set dry_run=false + db_generator.bat   # only if schema.json changed
- [Unity] Tools/UI Setup/Prefabs/X       # only if UIEditorSetup.cs structure changed
- [bat]   tools/subset_fonts.bat         # only if client_string.csv changed (after info_generator)
```

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
