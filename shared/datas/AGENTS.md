# shared/datas - Game Meta Data (CSV Sources)

## Nav
| path | domain |
|------|--------|
| `currency/` | Currency type definitions |
| `item/` | Item metadata |
| `reward/` | Reward group and entry tables referenced by FK from other domains |
| `ad/` | Rewarded-ad placement config |
| `shop/` | Shop catalog entries |
| `tutorial/` | Tutorial step definitions |
| `string/` | Localization strings, one file per language |
| `common/` | Shared enums, difficulty labels, other lookup tables |
| `avatar/` | Avatar unlock metadata |
| `stage/` | Chapter and stage progression metadata |

## CSV Format
```
Row 1: field names
Row 2: target scope - C | S | CS
Row 3: normalized type - int8/16/32/64, uint8/16/32/64, float, double, bool, string, string(N), EnumName
Row 4: constraints - PK, FK:[table], NN, UQ, IDX, AUTO
Row 5+: data
```

## Output (after `npm run gen:info`)
- `client/project-fill/Assets/Resources/Data/`
- `server/generated/data/`
- `server/generated/scripts/*/Xxx.g.cs`
- `client/.../Data/Generated/`
- `IStaticDataService.g.cs`
- `StaticDataService.g.cs`

## Normalization Rules
- 1 CSV = 1 entity type; no multi-entity tables.
- Rewards by reference: use `reward_group_id`; never inline reward columns.
- Event tables split by type; never use `event_type` column to mix types.
- Enums in `common/`; all enum/label tables live there.
- FK naming: always `{entity}_id`, e.g. `reward_group_id`, `item_id`.
- `_` prefix files/dirs are skipped by all generators.
- Signal Sort is the current baseline. Do not add turn-limit, stamina, tube/bottle sort, or non-circuit data unless the design SoT (signal_sort_system_design_kr.md) changes first.

## Cross-refs
- Gen output: `client/project-fill/Assets/Resources/Data/`, `server/generated/data/`
- Consumed by: `ProjectFill.Infrastructure.StaticData.StaticDataService`
- Consumed by: `client/project-fill/Assets/Scripts/Services/`
