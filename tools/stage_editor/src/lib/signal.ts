// Mirrors client InGame SignalType / LaneKind. Index = enum value.
// Colors/glyphs match SignalTypeExtensions so the editor preview reads like the runtime board.

export const SIGNAL_COUNT = 10;

export const SIGNAL_NAMES = [
  'Red', 'Blue', 'Green', 'Yellow', 'Purple',
  'Cyan', 'Orange', 'Magenta', 'Lime', 'Teal',
];

export const SIGNAL_GLYPHS = ['R', 'B', 'G', 'Y', 'P', 'C', 'O', 'M', 'L', 'T'];

// SignalTypeExtensions.Colors (0..1 floats) → css rgb.
export const SIGNAL_COLORS = [
  'rgb(255,69,87)',   // Red
  'rgb(69,135,255)',  // Blue
  'rgb(69,255,135)',  // Green
  'rgb(255,214,69)',  // Yellow
  'rgb(181,69,255)',  // Purple
  'rgb(69,255,255)',  // Cyan
  'rgb(255,135,69)',  // Orange
  'rgb(255,77,184)',  // Magenta
  'rgb(168,255,77)',  // Lime
  'rgb(51,204,189)',  // Teal
];

export enum LaneKind { Normal = 0, Locked = 1, Blind = 2 }

export const LANE_KIND_CODE  = ['N', 'L', 'B'];               // csv encoding
export const LANE_KIND_LABEL = ['Normal', 'Locked', 'Blind'];

export function typeToGlyph(type: number): string {
  return SIGNAL_GLYPHS[type] ?? '?';
}

export function glyphToType(glyph: string): number {
  const idx = SIGNAL_GLYPHS.indexOf(glyph.toUpperCase());
  return idx < 0 ? -1 : idx;
}

export function kindToCode(kind: number): string {
  return LANE_KIND_CODE[kind] ?? 'N';
}

export function codeToKind(code: string): LaneKind {
  switch (code.toUpperCase()) {
    case 'L': return LaneKind.Locked;
    case 'B': return LaneKind.Blind;
    default:  return LaneKind.Normal;
  }
}

export function parseLaneKinds(s: string): LaneKind[] {
  return [...(s ?? '')].map(codeToKind);
}

export function laneKindsToString(kinds: number[]): string {
  return kinds.map(kindToCode).join('');
}

// lock_unlock csv: one char per lane — glyph for Locked lanes, '.' otherwise.
export function parseLockUnlock(s: string, laneCount: number): number[] {
  const out: number[] = [];
  for (let i = 0; i < laneCount; i++) {
    const ch = s?.[i] ?? '.';
    const t = ch === '.' ? -1 : glyphToType(ch);
    out.push(t);
  }
  return out;
}

export function lockUnlockToString(unlock: number[], kinds: number[]): string {
  return kinds
    .map((k, i) => (k === LaneKind.Locked && unlock[i] >= 0 ? typeToGlyph(unlock[i]) : '.'))
    .join('');
}

export function relayOrderToTypes(s: string): number[] {
  return [...(s ?? '')].map(glyphToType).filter(t => t >= 0);
}

export function typesToRelayOrder(types: number[]): string {
  return types.map(typeToGlyph).join('');
}
