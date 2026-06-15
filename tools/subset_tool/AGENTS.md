# subset_tool - Font Subset Automation

## Files
| file | role |
|---|---|
| `config.json` | Maps language codes to source/target font files for subset generation |
| `subset_fonts.js` | CLI tool; reads localization CSVs, invokes `fontTools.subset`, overwrites changed target font files |
| `sources/` | Original full-size font files used as subset inputs |

## Symbols
| symbol | kind | note |
|---|---|---|
| `main()` | function | loads config, validates paths, builds language charsets, processes each font entry |
| `subsetFont(entry,...)` | function | creates temporary subset font and overwrites target only when content hash changed |
| `parseCsv(content)` | function | quote-aware CSV parser for localization source files |
| `buildLanguageCharsets(config)` | function | reads all string CSVs and builds per-language character sets |

## Font Coverage
| font | languages | note |
|------|-----------|------|
| `Silver.ttf` | EN, ES, FR, DE, PT, RU, TH, IT, TR, ID | Latin + Cyrillic + Thai — full coverage confirmed |
| `Galmuri11-Bold.ttf` | KO | Hangul 100% — Korean-only pixel font |
| `unifont-17.0.04.otf` | ALL | Unicode BMP fallback — 58,910 glyphs |

## Cross-refs
- Depends on: `template.ini [font-subset]`, `shared/datas/string/*.csv`
- Consumed by: release build pipeline before Unity Android build

## Rules
- Source fonts in `sources/` must be original full fonts, never generated subset outputs.
- Target font files are overwritten intentionally; Unity `.meta` files are preserved by keeping filenames stable.
- SDF assets are not regenerated here; current TMP dynamic font assets reuse the same source font GUID.
- Run via `npm run font:subset` or `tools/subset_fonts.bat`.
