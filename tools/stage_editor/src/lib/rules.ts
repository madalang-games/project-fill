// TS port of the runtime Signal Sort board model (client InGame Board/SlotLane/Chip).
// Used for in-editor playtest / simulate only — generation + solving live in the C# CLI.
// Keep move/absorb/relay/unlock semantics in sync with Board.cs if the runtime rules change.

import type { ChipData, LaneData, GenerateResult } from '../types/stage';
import { LaneKind } from './signal';

export const CAPACITY = 4;

export interface Lane {
  kind: number;
  locked: boolean;
  unlockType: number;
  pending: boolean;
  chips: ChipData[];
}

export interface BoardState {
  lanes: Lane[];
  relayOrder: number[];
  relayProgress: number;
  completedSets: number;
  totalSets: number;
  moveCount: number;
}

export function fromLanes(lanes: LaneData[], totalSets: number, relayOrder: number[]): BoardState {
  return {
    lanes: lanes.map(l => ({
      kind: l.kind,
      locked: l.locked,
      unlockType: l.unlockType,
      pending: false,
      chips: l.chips.map(c => ({ ...c })),
    })),
    relayOrder: [...relayOrder],
    relayProgress: 0,
    completedSets: 0,
    totalSets,
    moveCount: 0,
  };
}

export function fromGenerateResult(g: GenerateResult, relayOrder: number[]): BoardState {
  return fromLanes(g.lanes, g.types, relayOrder);
}

export function cloneBoard(b: BoardState): BoardState {
  return {
    lanes: b.lanes.map(l => ({ ...l, chips: l.chips.map(c => ({ ...c })) })),
    relayOrder: [...b.relayOrder],
    relayProgress: b.relayProgress,
    completedSets: b.completedSets,
    totalSets: b.totalSets,
    moveCount: b.moveCount,
  };
}

export function isCleared(b: BoardState): boolean {
  return b.completedSets >= b.totalSets;
}

function laneTop(l: Lane): ChipData | null {
  return l.chips.length > 0 ? l.chips[l.chips.length - 1] : null;
}

function laneComplete(l: Lane): boolean {
  if (l.chips.length !== CAPACITY) return false;
  const t = l.chips[0].type;
  return l.chips.every(c => c.type === t);
}

function canAccept(l: Lane, c: ChipData): boolean {
  if (l.locked || l.pending || l.chips.length >= CAPACITY) return false;
  if (c.overload && l.chips.length === 0) return false;
  if (l.chips.length === 0) return true;
  return l.chips[l.chips.length - 1].type === c.type;
}

export function canMoveTo(b: BoardState, from: number, to: number): boolean {
  if (from === to || from < 0 || to < 0 || from >= b.lanes.length || to >= b.lanes.length) return false;
  const src = b.lanes[from];
  const top = laneTop(src);
  if (!top || src.pending) return false;
  return canAccept(b.lanes[to], top);
}

// Chips a Move(from,to) would relocate: contiguous same-type run from source top, capped by dest free.
export function movableCount(b: BoardState, from: number, to: number): number {
  if (!canMoveTo(b, from, to)) return 0;
  const src = b.lanes[from];
  const type = laneTop(src)!.type;
  let free = CAPACITY - b.lanes[to].chips.length;
  let n = 0;
  for (let i = src.chips.length - 1; i >= 0 && n < free; i--) {
    if (src.chips[i].type !== type) break;
    n++;
  }
  return n;
}

// Batch move + cascade absorb. Returns the mutated clone and the lanes absorbed by this move.
export function applyMove(
  prev: BoardState,
  from: number,
  to: number,
): { board: BoardState; absorbed: { lane: number; type: number }[] } | null {
  if (!canMoveTo(prev, from, to)) return null;
  const b = cloneBoard(prev);
  const src = b.lanes[from];
  const dst = b.lanes[to];
  const type = laneTop(src)!.type;
  while (src.chips.length > 0 && laneTop(src)!.type === type && canAccept(dst, laneTop(src)!)) {
    dst.chips.push(src.chips.pop()!);
  }
  b.moveCount++;
  const absorbed = resolveCompletions(b);
  return { board: b, absorbed };
}

function resolveCompletions(b: BoardState): { lane: number; type: number }[] {
  const absorbed: { lane: number; type: number }[] = [];
  const hasRelay = b.relayOrder.length > 0;
  let changed = true;
  while (changed) {
    changed = false;
    for (let i = 0; i < b.lanes.length; i++) {
      const lane = b.lanes[i];
      if (!laneComplete(lane)) continue;
      const type = lane.chips[0].type;
      if (hasRelay) {
        if (b.relayProgress < b.relayOrder.length && b.relayOrder[b.relayProgress] === type) {
          lane.pending = false;
          absorb(b, lane, type, absorbed, i);
          b.relayProgress++;
          changed = true;
        } else if (!lane.pending) {
          lane.pending = true;
        }
      } else {
        absorb(b, lane, type, absorbed, i);
        changed = true;
      }
    }
  }
  return absorbed;
}

function absorb(b: BoardState, lane: Lane, type: number, out: { lane: number; type: number }[], index: number): void {
  lane.chips = [];
  b.completedSets++;
  out.push({ lane: index, type });
  for (const l of b.lanes) {
    if (l.locked && l.unlockType === type) l.locked = false;
  }
}

export function isHardStuck(b: BoardState): boolean {
  for (let i = 0; i < b.lanes.length; i++) {
    const src = b.lanes[i];
    const top = laneTop(src);
    if (!top || src.pending) continue;
    for (let j = 0; j < b.lanes.length; j++) {
      if (i === j) continue;
      if (canAccept(b.lanes[j], top)) return false;
    }
  }
  return true;
}

export function selectable(b: BoardState, lane: number): boolean {
  const l = b.lanes[lane];
  return !!l && l.chips.length > 0 && !l.pending && !l.locked;
}

// ── Solver (mirrors CLI BatchSolver) — exact-shortest BFS over canonical states ──
// Returns the minimal clearing batch-move sequence (no wasteful moves), so Simulate replays the
// same clean, optimal path the generator scored against. Canonical (lane-order-invariant) keys
// collapse lane permutations, keeping the search tractable.

function canonicalKey(b: BoardState): string {
  const sigs: string[] = [];
  for (const l of b.lanes) {
    let s = l.locked ? 'L' + String(l.unlockType) : l.pending ? 'P' : '.';
    for (const c of l.chips) { s += String(c.type); if (c.overload) s += '!'; }
    sigs.push(s);
  }
  sigs.sort();
  return sigs.join('/') + '#' + b.relayProgress;
}

function enumerateMoves(b: BoardState): [number, number][] {
  const out: [number, number][] = [];
  for (let i = 0; i < b.lanes.length; i++) {
    const src = b.lanes[i];
    const top = src.chips[src.chips.length - 1];
    if (!top || src.pending) continue;
    for (let j = 0; j < b.lanes.length; j++) {
      if (i === j) continue;
      if (canAccept(b.lanes[j], top)) out.push([i, j]);
    }
  }
  return out;
}

export function solve(start: BoardState, nodeCap = 120000): [number, number][] | null {
  if (isCleared(start)) return [];
  const startKey = canonicalKey(start);
  // key → { prev: predecessor key | null, move }
  const parent = new Map<string, { prev: string | null; move: [number, number] }>();
  parent.set(startKey, { prev: null, move: [0, 0] });
  const queue: { board: BoardState; key: string }[] = [{ board: start, key: startKey }];

  let nodes = 0;
  let head = 0;
  while (head < queue.length) {
    if (++nodes > nodeCap) return null;
    const { board: cur, key: curKey } = queue[head++];
    for (const [from, to] of enumerateMoves(cur)) {
      const r = applyMove(cur, from, to);
      if (!r) continue;
      const key = canonicalKey(r.board);
      if (parent.has(key)) continue;
      parent.set(key, { prev: curKey, move: [from, to] });
      if (isCleared(r.board)) return reconstruct(parent, key);
      queue.push({ board: r.board, key });
    }
  }
  return null;
}

function reconstruct(
  parent: Map<string, { prev: string | null; move: [number, number] }>, endKey: string,
): [number, number][] {
  const moves: [number, number][] = [];
  let key: string | null = endKey;
  while (key !== null) {
    const node = parent.get(key)!;
    if (node.prev === null) break;
    moves.push(node.move);
    key = node.prev;
  }
  moves.reverse();
  return moves;
}

export { LaneKind };
export type { LaneData };
