# tools/validate

## Files
| file | role |
|------|------|
| `validate.js` | Read-only cross-source validator; never writes generated outputs. `npm run gen:validate` (`--fix` = advisory suggestions only) |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `parseEnums` | function | Parses `shared/contracts/GameTypes/*.cs` → `Map<enumName, Set<member>>` |
| `parseSchemaColumns` | function | Parses `server/db/schema.json` → `Map<table, Map<col, type>>` |
| `readCsv` | function | Lightweight 4-header-row CSV reader (own parser; does not import info_generator to avoid its `main()` side effect) |

## Checks
1. **enum** — CSV enum-typed cell values must be members of a `GameTypes` enum (integer literals allowed; empty = nullable skip)
2. **fk** — `FK:<path>` resolves to a CSV and every value exists in the target's PK∪UQ key set; `FK:<path>.<col>` targets a specific column
3. **schema** — a CSV's column type must match the same-named column in `schema.json` when a CSV basename and table name overlap

## Rules
- Read-only — NEVER edits sources. `--fix` prints suggested edits only (auto-adding enum members would entrench typos and pollute the SoT).
- Type/enum/path config comes from `cfg` (`config-loader`); no hardcoded paths.
- Exit 1 on any problem (CI drift gate).

## Cross-refs
- Depends on: `tools/config-loader.js`, `shared/datas/**`, `shared/contracts/GameTypes/`, `server/db/schema.json`
- Consumed by: `.github/workflows/gen-check.yml`, `package.json` (`gen:validate`)
