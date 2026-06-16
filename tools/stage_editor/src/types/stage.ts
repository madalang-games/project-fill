// A chip on the board. `overload` chips (Ch5) cannot be placed onto an empty lane.
export interface ChipData {
  type: number;       // SignalType index 0..9
  overload: boolean;
}

// One lane (slot tube). chips listed bottom → top.
export interface LaneData {
  kind: number;       // LaneKind
  locked: boolean;
  unlockType: number; // SignalType whose set unlocks this lane (Locked lanes)
  chips: ChipData[];
}

// stage.csv row — Signal Sort definition (gimmicks) + explicit compact board layout.
// The board is stored directly (see board-codec); rendering decodes it with no generator call.
export interface StageRow {
  stage_id: number;
  chapter_id: number;
  stage_order: number;
  difficulty: number;       // 0 Easy / 1 Normal / 2 Hard
  reward_group_id: number;
  types: number;            // signal types = number of sets
  lane_kinds: string;       // per-lane kind codes, e.g. "NNNNNL"
  lock_unlock: string;      // per-lane unlock glyph or '.', e.g. ".....R"
  overload_type: number;    // -1 none, else SignalType index
  relay_order: string;      // glyph sequence e.g. "RBGYP", "" = none
  board: string;            // compact chip layout (see board-codec); "" = not yet generated
}

export interface ChapterRow {
  chapter_id: number;
  display_order: number;
  unlock_chapter_id: number | null;
  bg_theme_id: number;
}

export type StageMeta = StageRow;

// Generator request sent to /api/generate-board (camelCase → C# CLI contract).
export interface GeneratorConfig {
  types: number;
  laneKinds: string;
  lockUnlock: string;
  overloadType: number;
  relayOrder: string;
  difficulty: number;
  maxAttempts: number;
}

// Result returned by the generator (best scored candidate) or reproduce mode.
export interface GenerateResult {
  lanes: LaneData[];
  types: number;
  seed: number;
  attempts: number;
  solveLength: number;
  verifiedSolution: string; // "from,to;from,to;..."
  score: number;
}
