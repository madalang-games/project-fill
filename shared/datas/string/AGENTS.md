# shared/datas/string — Localization String Tables

## Files
| file | PK | scope | role |
|------|----|-------|------|
| `client_string.csv` | `stringId` | C | UI text labels, button labels, popup titles |
| `error_messages.csv` | `errorCode` | C | Client-facing server error code translations |

## Key Naming Convention
`<screen>.<component>[.<variant>]`

| prefix | used for |
|--------|---------|
| `app.*` | App-level constants (title, tagline) |
| `boot.*` | Boot scene |
| `common.*` | Shared across screens (buttons, labels) |
| `error.*` | Generic client-side error messages |
| `nav.*` | Bottom navigation bar |
| `popup.<name>.*` | Popup/overlay specific strings |

Examples:
```
common.btn_retry
popup.pause.title
popup.pause.btn_resume
popup.settings.bgm
error.network_check
```

## Language Columns
Defined by `tools/subset_tool/config.json`. Current: `EN KO ZH_CN ZH_TW JA RU ES PT FR DE TH AR IT TR ID`

**MVP**: EN and KO filled. All other language columns are intentionally blank.
`LocalizationService` falls back to EN when the current language column is empty.

## Error Toast Convention (Server Error → Client Display)

Server sends `{ "code": "ERROR_CODE" }` only (no `message` field).  
Client resolves display text via `LocalizationService`:

```
raw response body
  └─ LocalizationService.GetErrorFromResponse(text)   // parses JSON, extracts "code"
       └─ LocalizationService.GetError(code)           // looks up error_messages.csv
            └─ fallback: returns code string if not found
```

### error_messages.csv responsibilities
- One row per server error code that the client may display to users.
- `errorCode` column must match `ErrorCodes.*` constant in `server/src/ProjectFill.Application/Common/ErrorCodes.cs` exactly.
- EN + KO minimum; other language columns left blank (fallback to EN).

### When adding a new server error code
1. Add the constant to `ErrorCodes.cs` (server).
2. Add a row to `error_messages.csv` (EN + KO).
3. Run `tools/info_generator.bat` + `tools/subset_fonts.bat` after changes.

### client_string.csv vs error_messages.csv
| use case | file |
|----------|------|
| Server error code toast | `error_messages.csv` via `GetError(code)` |
| UI label, button, popup text | `client_string.csv` via `Get(key)` |

## Rules
- Keys use dot-namespace: `<screen>.<component>[.<variant>]`
- No numeric keys, no ALL_CAPS keys (use descriptive names)
- `_fmt` suffix on keys containing `{0}` format parameters
- No embedded newlines in CSV cell values (CsvLoader splits on `\n`)
- MVP blank columns: do NOT add NN constraint to non-EN/KO columns
- `error_messages.csv` errorCode must match server-returned error codes exactly
- **Font Subsetting**: After adding/modifying keys in `client_string.csv` or `error_messages.csv`, you must run `tools/info_generator.bat` followed by `tools/subset_fonts.bat` to rebuild font subsets containing the new characters.

## Cross-refs
- Gen output: `client/project-fill/Assets/Resources/Data/string/`
- Consumed by: `Game.Services.LocalizationService`
- Depends on: `tools/subset_tool/config.json` (language list)
