# Platform Auth Reference

Source of truth: `platform/docs/refs/auth.md`

## JWT Claims
| claim | type | description |
|-------|------|-------------|
| `sub` | string | platform_pid (24-char public ID) |
| `aud` | string | `platform-games` |
| `iss` | string | platform auth URL |
| `session_id` | string | platform session id, owned by platform-auth |

## Endpoints (platform-auth service)
| method | path | description |
|--------|------|-------------|
| GET | `/.well-known/jwks.json` | Public key set for offline JWT validation |
| GET | `/api/internal/users/{pid}/uid` | Internal server lookup from platform PID to uid |
| POST | `/auth/guest` | Guest login; returns access + refresh token |
| POST | `/auth/refresh` | Refresh token exchange |
| POST | `/auth/logout` | Invalidate refresh token |

## Game Server Rules
- Project Fill validates access JWTs statelessly through platform-auth JWKS.
- `sub` is stored as `players.platform_pid`; it is not numeric and is not used as uid.
- Internal uid is resolved server-side and exposed only as an in-process `user_id` claim.
- Refresh, logout, session-family state, account linking, and token revocation belong to platform-auth.
- Project Fill does not maintain `sessions.active`.
