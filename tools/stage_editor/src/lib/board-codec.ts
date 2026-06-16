// Compact board encoding stored in stage.csv `board` column.
// Fixed 4 chars per lane (Capacity), no delimiters: read in groups of 4, lane order = csv order.
//   uppercase glyph = normal chip   (R B G Y P C O M L T)
//   lowercase glyph = overload chip  (overload only ever applies to the overload_type)
//   '-'             = empty slot     (an empty lane = "----")
// Lane kind / locked / unlock come from the lane_kinds + lock_unlock columns, not from here.

import type { LaneData, ChipData } from '../types/stage';
import { SIGNAL_GLYPHS, LaneKind } from './signal';

const SLOTS = 4;

export function encodeBoard(lanes: LaneData[]): string {
  return lanes
    .map(l => {
      let s = '';
      for (let i = 0; i < SLOTS; i++) {
        const c = l.chips[i];
        if (!c) { s += '-'; continue; }
        const g = SIGNAL_GLYPHS[c.type] ?? '?';
        s += c.overload ? g.toLowerCase() : g;
      }
      return s;
    })
    .join('');
}

export function decodeBoard(board: string, kinds: number[], unlock: number[]): LaneData[] {
  const lanes: LaneData[] = [];
  for (let i = 0; i < kinds.length; i++) {
    const seg = board.slice(i * SLOTS, i * SLOTS + SLOTS);
    const chips: ChipData[] = [];
    for (const ch of seg) {
      if (ch === '-') continue;
      const idx = SIGNAL_GLYPHS.indexOf(ch.toUpperCase());
      if (idx < 0) continue;
      const overload = ch === ch.toLowerCase() && ch !== ch.toUpperCase();
      chips.push({ type: idx, overload });
    }
    lanes.push({
      kind: kinds[i],
      locked: kinds[i] === LaneKind.Locked,
      unlockType: unlock[i] >= 0 ? unlock[i] : 0,
      chips,
    });
  }
  return lanes;
}

export function hasBoard(board: string | undefined | null): boolean {
  return !!board && /[^-]/.test(board);
}
