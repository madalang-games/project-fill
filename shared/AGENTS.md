# shared

## Nav
| path | role |
|------|------|
| `contracts/` | C# DTO contracts (netstandard2.1, Unity-compatible) | → `contracts/AGENTS.md` |
| `datas/` | CSV game meta data — source for info_generator | → `datas/AGENTS.md` |

## Rules
- NEVER edit generated outputs — edit source here and re-run generators

## Completion Gate
When any shared source is modified, FLAG the corresponding generator before closing task:

| Modified source | FLAG (user must run) | Note |
|-----------------|----------------------|------|
| `datas/**/*.csv` | `tools/info_generator.bat` | — |
| `datas/string/client_string.csv` | `tools/info_generator.bat` + `tools/subset_fonts.bat` | run subset_fonts after info_generator |
| `contracts/**/*.cs` | `tools/pkt_generator.bat` | — |
