# ProjectFill.Application/Common

## Files
| file | class | role |
|------|-------|------|
| `ErrorCodes.cs` | `ErrorCodes` | Stable client-visible error code constants |
| `GameApiException.cs` | `GameApiException` | Application exception carrying an error code |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `ErrorCodes.*` | constants | All client-visible codes; grouped by domain in file |
| `GameApiException.Code` | property | Transmitted to client via `ApiExceptionMiddleware` |
| `GameApiException(code, message)` | ctor | `message` is server-log only — never transmitted |

## Error Code Convention (Server → Client)

**Wire format** — server sends only `{ "code": "ERROR_CODE" }` (no `message` field).  
Clients display user-facing text by looking up the code in `error_messages.csv`.

### Adding a new error code
1. Check `ErrorCodes.cs` for an existing constant with the same semantics.  
2. If none exists, add a constant here **and** a row in `shared/datas/string/error_messages.csv` (EN + KO minimum).  
3. Throw via `throw new GameApiException(ErrorCodes.YourCode, "internal description for logs");` — the second arg is log-only.  
4. Never throw with a raw inline string; always use an `ErrorCodes` constant.

### `message` field policy
- `GameApiException(code, message)` — `message` goes to server logs only.
- `ApiExceptionMiddleware` writes `ErrorResponse { Code }` only; `message` is never serialized to the wire.
- Rationale: client text is owned by `error_messages.csv` + `LocalizationService`; server message strings are untranslated and leak internals.
