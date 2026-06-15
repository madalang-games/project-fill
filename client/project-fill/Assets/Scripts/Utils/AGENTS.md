# Utils — Shared stateless helpers

## Files
| file | class | role |
|------|-------|------|
| `CsvLoader.cs` | `CsvLoader` | Loads typed arrays from Resources CSV via reflection; supports patch override |
| `ArcLayoutGroup.cs` | `ArcLayoutGroup` | MonoBehaviour; arranges child RectTransforms in circular arc |
| `VirtualizedScrollRect.cs` | `VirtualizedScrollRect` | MonoBehaviour; highly optimized vertical scroll virtualization pooling UI items |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `VirtualizedScrollRect.Init(int,Action<int,GameObject>)` | method | Initializes scroll virtualization and binds visibility callback |
| `CsvLoader.Load<T>(string)` | method | static; checks `persistentDataPath/data_patch/{path}.csv` first, falls back to `Resources.Load`; supports sbyte/byte/short/ushort/int/uint/long/ulong/float/double/bool/string/Enum |
| `CsvLoader.WritePatchFile(string,string)` | method | static; writes CSV text to patch dir |
| `CsvLoader.GetPatchedMetaHash()` | method | static; reads `data_patch/meta_hash.txt` |
| `CsvLoader.SavePatchedMetaHash(string)` | method | static; writes metaHash to patch file |
| `CsvLoader.ClearPatch()` | method | static; deletes entire `data_patch/` dir |
| `ArcLayoutGroup.radius` | field | circle radius; controls arc height and item spacing |
| `ArcLayoutGroup.arcSpanDegrees` | field | total angle span (1–360°) |
| `ArcLayoutGroup.invertArch` | field | false = upward arch; true = downward arch |
| `ArcLayoutGroup.Rebuild()` | method | recalculates all child positions/rotations; auto-called OnEnable + OnValidate |

## Rules
- All classes are stateless utilities — no game data dependencies
- CsvLoader field names in T must exactly match CSV header row (row 1)
