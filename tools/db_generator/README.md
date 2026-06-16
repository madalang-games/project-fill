# Project Link DB Generator

Schema-driven MySQL + EF Core generator for Project Link.

## Source
- `server/db/schema.json` is the source of truth.
- Generated C# files are written to `server/src/ProjectLink.Infrastructure/Generated/`.
- Migration SQL is written to `server/db/migrations/{timestamp}_schema_sync.sql` when DB diff detects changes.

## Commands
```bash
npm run gen:orm
npm run gen:orm -- --generate-only
npm run gen:orm -- --allow-drops
```

`--generate-only` skips DB connection and regenerates C# plus review SQL.
`--allow-drops` is required before DROP statements can be executed against a dev DB.

## Migration behavior
Diff is computed against live `INFORMATION_SCHEMA`:
- **Added column** → `ADD COLUMN` (applied).
- **Changed column** (type or nullability differ) → `MODIFY COLUMN`, **commented out by default** — review and apply manually, or pass `--allow-drops`. MODIFY can truncate data. Column `DEFAULT` changes are not diffed.
- **Removed column / table** → `DROP`, commented out unless `--allow-drops`.

Applied migrations are recorded in a tool-managed `schema_migrations` ledger table (`version`, `applied_at`). The ledger is excluded from schema diffs and never dropped.

## Config
The generator reads paths and behavior from `template.ini`, then DB credentials from `.env.dev` or `.env.prod` via `tools/config-loader.js`.

Important settings:
- `[paths].db_schema`
- `[paths].migrations_dir`
- `[paths].orm_generated_dir`
- `[orm-gen].dry_run`

The **connection target DB is the env `DB_NAME`** (the database docker actually creates and grants), not `schema.json`'s `database` field. `schema.database` is advisory: if it differs from `DB_NAME` the generator warns (DB names are case-sensitive on Linux MySQL). Keep them aligned.

## Generated C# Surface
Each table produces one `{Table}Db.g.cs` containing:
- `{Table}Row`
- `{Table}DbConfiguration`
- `{Table}Db`
- `Schema` constants for the table and columns
- `FindAsync(...)` in primary-key schema order
- `FindBy{Column}Async(...)` for single-column unique keys
- FK navigation properties on FK-owning rows
- bidirectional `JoinWith...()` helpers
- `InsertIgnoreAsync(row, ct)` — only when table has `"conflict": "ignore"` in schema.json

`AppDbContext.g.cs` exposes internal `DbSet` properties and public DBObject properties. Application code should use DBObjects rather than direct `DbSet` access.

## Conflict Resolution (`"conflict"` field)
Set `"conflict": "ignore"` on a table to generate `InsertIgnoreAsync(row, ct)`.

Uses `INSERT IGNORE INTO ...` via `ExecuteSqlInterpolatedAsync`. Auto-increment columns are excluded from the insert.

**When to use**: any table where an insert can race with a concurrent insert of the same unique/PK key — specifically inserts inside ASP.NET Core **middleware** (which runs before `UserSerializeFilter` and has no per-user lock). See `project-link/AGENTS.md §Middleware-Level Concurrent Insert Risk`.

## Prohibited
- Editing generated `*.g.cs` files
- Hardcoded table/column identifiers in raw SQL
- `FOR UPDATE` / `FOR UPDATE NOWAIT`
- EF migrations
