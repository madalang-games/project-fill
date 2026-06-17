# stage_editor — Next.js Signal Sort Stage Editor

Standalone dev tool. Authors Signal Sort stages and persists their definition + explicit board layout to
`shared/datas/stage/stage.csv` via Next.js API routes. Based on project-flood's stage_editor (same
UI shell + scored-generation paradigm), adapted from flood's grid game to project-fill's lane/chip game.

## Model
A stage = a definition (`types`, per-lane kinds, gimmicks) + an explicit `board` (compact chip layout,
see `lib/board-codec`). **Generate** runs the C# CLI (scored: sample many seeds, score difficulty, keep
best) and the editor encodes the winning candidate's lanes into the `board` column. The board is stored
explicitly — render/playtest decode it locally (no generator call, no algorithm-parity coupling).
Authoring flow: edit definition → **Generate** → preview / playtest / simulate → **Save** (writes def + board).

**Generate modes** (GeneratorPanel `Use generate definition` toggle):
- OFF → generate from the per-stage Definition/Metadata panels (explicit painted gimmicks).
- ON → generate from the GeneratorPanel block itself (types / lanes / difficulty / lock·blind counts /
  overload·relay flags); the right Definition panel is **ignored**. Gimmicks are count-based random
  (random positions/colors; overload·relay always included with random value when checked).

In either mode the CLI **echoes** the resolved `laneKinds`/`lockUnlock`/`overloadType`/`relayOrder` (and
`types`) in its result; the editor writes them back into `meta` (with `board`) so the stored def matches the
generated board. In generate-def mode it also adopts the block's `types`/`difficulty` (+ reward). Generator
settings live in page-level `genSettings` (persist across stage select / New).

## Nav
| path | role |
|------|------|
| `src/app/page.tsx` | Editor orchestrator — all state, handlers, 3-column layout |
| `src/app/layout.tsx` | Root layout + Tailwind |
| `src/app/api/stages/route.ts` | GET all / POST new stage |
| `src/app/api/stages/[id]/route.ts` | GET / PUT / DELETE by stage_id |
| `src/app/api/chapters/route.ts` | GET all / POST new chapter |
| `src/app/api/chapters/[id]/route.ts` | DELETE chapter by id |
| `src/app/api/generator-defaults/route.ts` | GET defaults from `template.ini [stage-editor-generator]` |
| `src/app/api/generate-board/route.ts` | POST → runs `tools/stage_generator` CLI (scored generation) |
| `src/components/ChapterPanel.tsx` | Left top — chapter list select/create/delete |
| `src/components/StageList.tsx` | Left bottom — stages in selected chapter |
| `src/components/BoardView.tsx` | Center — lane/chip board render + playtest tap; relay-order strip above board (glyph sequence, absorbed steps dimmed via `relayProgress`) |
| `src/components/DefinitionPanel.tsx` | Right — edit types, lane kinds, lock/overload/relay gimmicks |
| `src/components/MetadataPanel.tsx` | Bottom — order, difficulty→reward, board status readout |
| `src/components/GeneratorPanel.tsx` | Bottom — max attempts + `Use generate definition` toggle (types/lanes/difficulty/lock·blind counts/overload·relay) + Generate (scored) + status; controlled by page `genSettings` |
| `src/components/PlaytestPanel.tsx` | Bottom — playtest/reset/stop, Simulate (step ◀▶ replay of solution), Save |
| `src/lib/signal.ts` | SignalType/LaneKind colors, glyphs, csv encode/decode helpers |
| `src/lib/board-codec.ts` | `board` column encode/decode (4 chars/lane; lower=overload; `-`=empty) |
| `src/lib/rules.ts` | TS port of runtime Board/SlotLane (playtest) + DFS `solve` (simulate) |
| `src/lib/csv.ts` | stage.csv / chapter.csv read/write (4-line header) |
| `src/lib/ini.ts` | Minimal INI parser for generator defaults |
| `src/types/stage.ts` | StageRow, ChapterRow, ChipData, LaneData, GeneratorConfig, GenerateResult |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `StageRow` | type | stage.csv row (12 cols): id/chapter/order/difficulty/**par_moves**/reward + types/lane_kinds/lock_unlock/overload_type/relay_order/board. `par_moves` (server scope S) = optimal solveLength, set on Generate |
| `GeneratorConfig` | type | CLI request (camelCase): types/laneKinds/lockUnlock/overloadType/relayOrder/difficulty/maxAttempts + randomize: lockCount/blindCount/randomizeGimmicks/randomOverload/randomRelay |
| `GenSettings` | type | Editor-level generator settings: maxAttempts + `useGenerateDef` toggle + generate-def block (types/laneCount/difficulty/lockCount/blindCount/overload/relay); page state, persists across stage select / New |
| `board-codec.encodeBoard/decodeBoard` | fn | lanes ↔ `board` string; `hasBoard()` = non-empty layout |
| `rules.applyMove` | fn | Batch move + cascade absorb (mirrors runtime `Board.Move`) |
| `rules.solve` | fn | Exact-shortest **BFS** over canonical states → minimal clearing move sequence, no wasteful moves (mirrors CLI `BatchSolver`); used by Simulate |
| `rules.fromLanes` | fn | decoded lanes → playable `BoardState` |
| Simulate | feature | `solve` the current board in TS, replay move-by-move (page builds `BoardState[]`; ◀▶ steps) |
| `signal.SIGNAL_COLORS/GLYPHS` | const | Mirror `SignalTypeExtensions`; glyph order R B G Y P C O M L T |
| `DIFFICULTY_REWARD` | const | `{0:2001,1:2002,2:2003}` — difficulty → reward_group_id |

## Rules
- Launch via `tools/stage_editor.bat` (publishes the CLI, sets `PROJECT_ROOT`, binds `[::1]:3000`,
  logs to `tools/logs/stage_editor-*.log`), or `npm run dev` here with `PROJECT_ROOT` = project-fill root.
- CSV/INI/CLI paths resolve via `PROJECT_ROOT` (defaults to `cwd/../..`).
- Boards are **generated, never hand-painted** — any definition change clears the `board`, forcing a
  re-Generate (keeps stages solvable-by-construction).
- `board-codec` must stay in sync with the runtime `BoardCodec.cs` (same 4-char/lane format) so the editor
  preview matches the in-game render exactly.
- `lib/rules.ts` must mirror the runtime Board rules (CanAccept, batch Move, relay/locked absorb) — update
  both if rules change.
- After editing `stage.csv` through the editor, FLAG `tools/info_generator.bat` to regenerate runtime data.
- NEW_DIR under `src/`: create `AGENTS.md` + update Nav.

## Cross-refs
- Depends on: `tools/stage_generator` (C# CLI), `shared/datas/stage/{stage,chapter}.csv`, `template.ini`
- Gen output: none (writes source `stage.csv` directly; `info_generator` propagates to runtime)
