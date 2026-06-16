# tools - Automation Pipeline

## Nav
| file/dir | role |
|----------|------|
| `config-loader.js` | Loads required `template.ini` + `.env.dev`/`.env.prod` + `types.json`; exposes paths, namespaces, `cfg.types`, `cfg.infoGenSkip` |
| `types.json` | **Shared type registry** — normalized type → `{csharp, mysql}`. SoT for type mapping; consumed by info_generator (csharp) + db_generator (csharp+mysql). NEVER duplicate maps in a generator |
| `info_generator/` | `shared/datas/**/*.csv` -> `*/generated/data/**/*.csv` + `StaticData/*.g.cs` + `IStaticDataService.g.cs` (diff-only + manifest stale cleanup) |
| `db_generator/` | `server/db/schema.json` -> DB CREATE/ALTER TABLE + migration SQL + generated EF data access (diff-only ORM writes). Detects column type/nullability changes (`MODIFY`, commented unless `--allow-drops`); records applied migrations in `schema_migrations` ledger |
| `pkt_generator/` | `shared/contracts/**/*.cs` -> configured client contracts output dir (diff-only, preserves `.meta`) |
| `validate/` | Read-only cross-source validator (enum membership / FK integrity / CSV↔DB type); `npm run gen:validate` | -> `validate/AGENTS.md` |
| `gen.js` | **Incremental orchestrator** — hashes each gen's source tree, runs only changed gens in order (`info`->`db`->`pkt`); `npm run gen` (incremental), `--all`, `--check`, `npm run gen:watch`. Cache: `.gen-cache/orchestrator.json` |
| `all_generator.bat` | Runs all gen steps in order (`info_generator` -> `db_generator` -> `pkt_generator`) |
| `info_generator.bat` | Runs info_generator only |
| `db_generator.bat` | Runs db_generator only |
| `pkt_generator.bat` | Runs pkt_generator only |
| `subset_tool/` | CSV-driven TMP source font subsetting before Unity release builds | -> `subset_tool/AGENTS.md` |
| `subset_fonts.bat` | Runs `subset_tool/subset_fonts.js` manually with logs; not part of `all_generator` |
| `stage_editor/` | Next.js Signal Sort stage editor (authors `stage/stage.csv` def+seed) | -> `stage_editor/AGENTS.md` |
| `stage_generator/` | .NET 8 CLI: scored Signal Sort board generation, invoked by stage_editor | -> `stage_generator/AGENTS.md` |
| `stage_editor.bat` | Publishes stage_generator + runs the stage_editor dev server (`[::1]:3000`); not part of `all_generator` |

## Rules
- **NEVER modify generated files manually** — always update the source and re-run the relevant generator
- ALL scripts read config from `config-loader.js` — never hardcode paths
- `npm run gen:check` verifies generated outputs are current without writing files
- Tools default to `.env.dev`; use `CONFIG_ENV=prod` or `ENV_FILE=.env.prod` for production values
- `_` prefix files/dirs are skipped by all gen tools
- Errors report as: `[tool] ERROR: <file>\n  <location>: <message>`
- On any error — print all errors, then `process.exit(1)`
- db_generator dry-run mode is required in `template.ini`; set `false` to execute
- Each generator script uses `require('../config-loader')` (one level up from its subdir)

## Adding a new gen tool
1. Create `tools/[name]/[name].js` — use `require('../config-loader')` for all config
2. Add `"gen:[name]": "node tools/[name]/[name].js"` to root `package.json` scripts
3. Add step to `all_generator.bat`
4. Register in `gen.js` `GENERATORS` (name, script path, source paths) for incremental runs
5. Update this Nav section
