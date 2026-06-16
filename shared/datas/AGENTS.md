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

## ID Convention

### `info_id` vs `id`
| Condition | PK column | Value | Decision rule |
|-----------|-----------|-------|---------------|
| CSV-managed static design data | `info_id` | Range-based offset (e.g. 1001, 2001) | Range encodes category; designer-assigned |
| Runtime DB records (not in CSV) | `id` | `AUTO` (auto_increment) | DB-managed; no semantic meaning needed |
| Fixed-index lookup (fixed max count, order is semantic) | `id` | Sequential (0, 1, 2…) | Only when total count is fixed and index IS the meaning (e.g. `color_palette` 0–15) |
| Detail/junction table in CSV | `id` | Sequential surrogate | Natural key is composite (parent_id + sort_order); surrogate OK |

### Range Design — active tables
| Range | Domain | Table |
|-------|--------|-------|
| `1001–1999` | IAP products | `shop/iap_product` |
| `101–199` | Tutorial group 1 (FirstLaunch stage 1) | `tutorial/tutorial_step` |
| `201–299` | Tutorial group 2 (FirstLaunch stage 2) | `tutorial/tutorial_step` |
| `2001–2999` | Stage-clear reward groups | `reward/reward_group` |
| `3001–3999` | Chapter-chest reward groups | `reward/reward_group` |
| `5001–5999` | IAP reward groups | `reward/reward_group` |

### Tables pending range design (currently sequential `id`)
`avatar`, `item`, `currency`, `iap_category`, `ad_placement`, `reward_source` — assign ranges before adding new rows.

### Enum mapping rules
- NEVER map all `info_id` values to an Enum (CSV changes → Enum drift)
- Range boundary Enum: validator/tool use only, NOT runtime branching
- `WellKnownItemId`-style Enum: only when code MUST hard-reference a specific record; keep minimal
- Runtime category branching: use a dedicated type/category column with enum values, not ID range

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
