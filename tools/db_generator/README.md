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

## Config
The generator reads paths and behavior from `template.ini`, then DB credentials from `.env.dev` or `.env.prod` via `tools/config-loader.js`.

Important settings:
- `[paths].db_schema`
- `[paths].migrations_dir`
- `[paths].orm_generated_dir`
- `[orm-gen].dry_run`

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
