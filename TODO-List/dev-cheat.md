# Dev Cheat System — Status

Spec: `docs/design/server-cheat-system-design.md`. Server-authoritative, dev-only.

## Implemented (server)
- **Phase 0** — `shared/contracts/Cheat` DTOs; `ProjectFillConfiguration.Dev` (`Enabled` = `GAME_ENV==dev`, `CheatWhitelist` = `DEV_CHEAT_WHITELIST`); `CheatCommandCatalog` (single source of truth).
- **Phase 1** — `CheatService` (gold/item/stage/tutorial/ad, reuses domain services); `CheatCommandParser` + `CheatDispatcher`; `DevCheatController POST /api/dev/cheat/command`; `DevOnlyMiddleware` (env 404) + `CheatWhitelistFilter` (PID 403); `Cheat*` event_logs (9001–9005).
- **Phase 1.5** — `CheatDocsPage` + `GET /api/dev/cheat/docs` (middleware-gated HTML).
- **Phase 3** — `CheatService.{Cosmetic,Achievement,Attendance}Async` (direct table forcing, same pattern as stage/tutorial cheats); catalog 3 rows; dispatcher 3 branches (`ParseStringTarget`/`ParseToggle`); `event_logs` 9006–9008.
- Tests: parser, gate (404/403), whitelist, per-domain ops (incl. cosmetic/achievement/attendance), dispatcher — `server_test.bat` green (91/91).

## Implemented (client)
- **Phase 2** — `UIEditorSetup.Cheat.cs::CreateCheatOverlay()` builds `Resources/Prefabs/UI/CheatOverlayView.prefab` (palette/Panel-3-layer/Btn-96px/TMP conventions, own Canvas sort 1000). `OutGame/Dev/CheatOverlayView.cs` (DEV-ONLY, `#if`-guarded) is prefab-bound and **dynamic-loaded** via `RuntimeInitializeOnLoadMethod` (`Resources.Load` + Instantiate, DDOL); backtick toggle; command + button modes (8 `CheatDomain` tabs + target/amount inputs + 4 pooled action buttons); log; Docs button (`GET /api/dev/cheat/docs` → temp html → `OpenURL`); prefix-based local refresh. `OutGame/Dev/DevEnums.cs` (`CheatDomain` mirror).
  - Labels inline English (TMP stringId=null → LocalizedText font-only) → no `client_string.csv`/font-subset churn.

## FLAG (user must run)
- `tools/all_generator.bat` — syncs `Cheat` contracts to client `Generated/Contracts` (pkt drift from Phase 0 still pending; Phase 2/3 added no new contracts).
- **(Unity)** `Tools/UI Setup/Prefabs/CheatOverlay` — generates `CheatOverlayView.prefab` (agent cannot run Unity GUI). Required before the client overlay loads.

## Remaining
- Ad-bypass enforcement: server stores `cheat:ad:{uid}`; wiring into the ad service/client `AdBypassEnabled` is open (design §11).
- Optional polish: overlay token-presence gate on toggle (design §7.1) — currently toggle always opens; commands just fail server-side (403/401) if unauthenticated.
