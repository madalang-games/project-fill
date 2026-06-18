# shared/datas/event

## Files
| file | class | role |
|------|-------|------|
| `weekly_mission_pool.csv` | `WeeklyMissionPool` | Weekly Mission Event rotation pool: 5+ missions, each a campaign-play aggregate condition + EP reward |
| `weekly_mission_track.csv` | `WeeklyMissionTrack` | EP milestone reward track (cumulative-EP thresholds → reward group) |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `WeeklyMissionPool.mission_id` | column | PK; string id assigned weekly into a mission set |
| `WeeklyMissionPool.condition_type` | column | `WeeklyMissionConditionType`; stage-clear-flow aggregate (StageClearCount/PerfectClearCount/BoosterlessClear/ChapterProgress/BestMovesRenew) |
| `WeeklyMissionPool.condition_value` | column | Target count for completion |
| `WeeklyMissionPool.ep_reward` | column | Event Points granted when the mission completes |
| `WeeklyMissionTrack.ep_threshold` | column | PK; cumulative EP needed to unlock the milestone |
| `WeeklyMissionTrack.reward_group_id` | column | FK reward group granted on milestone claim (gold/booster only — no cosmetics) |

## Rules
- Server assigns 5 missions per week (deterministic seed from `week_start_date`); pool may grow without code change.
- Rewards by reference (`reward_group_id`, range `7001–7099`); never inline reward columns.
- Track is gold/consumable only — cosmetic unlocks are owned by attendance/achievement systems, not this event.
- Progress is aggregated in the stage-clear flow (no submit endpoint); 5 mission EP sum to the final track threshold (1,200).

## Cross-refs
- Depends on: `shared/datas/reward/reward_group.csv`
- Depends on: `shared/contracts/GameTypes/GameEnums.cs` (`WeeklyMissionConditionType`)
- Consumed by: `ProjectFill.Application.Event.WeeklyMissionService`
- Consumed by: `Game.Services.WeeklyMissionApiService`
