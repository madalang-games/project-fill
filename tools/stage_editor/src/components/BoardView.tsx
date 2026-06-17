'use client';

import type { BoardState } from '../lib/rules';
import { CAPACITY, LaneKind } from '../lib/rules';
import { SIGNAL_COLORS, SIGNAL_GLYPHS, typeToGlyph } from '../lib/signal';

const BOARD_BG = '#1e1e2e';
const SLOT_BG  = '#2a2a3e';

interface Props {
  board: BoardState | null;
  selectedLane: number | null;
  isPlaytest: boolean;
  onLaneClick: (lane: number) => void;
}

function Chip({ type, overload, hidden }: { type: number; overload: boolean; hidden: boolean }) {
  if (hidden) {
    return (
      <div
        className="w-11 h-11 rounded-md flex items-center justify-center text-gray-500 text-lg font-bold"
        style={{ background: '#3a3a4e' }}
      >
        ?
      </div>
    );
  }
  return (
    <div
      className="w-11 h-11 rounded-md flex items-center justify-center font-bold text-base relative"
      style={{ background: SIGNAL_COLORS[type] ?? '#888', color: '#1a1a1a' }}
      title={`${SIGNAL_GLYPHS[type]}${overload ? ' (overload)' : ''}`}
    >
      {typeToGlyph(type)}
      {overload && (
        <span className="absolute -top-1 -right-1 text-[10px] bg-black/70 text-yellow-300 rounded-full w-4 h-4 flex items-center justify-center">
          ⚡
        </span>
      )}
    </div>
  );
}

export default function BoardView({ board, selectedLane, isPlaytest, onLaneClick }: Props) {
  if (!board) {
    return (
      <div className="text-gray-500 text-sm">No board — set the definition and press Generate.</div>
    );
  }

  return (
    <div className="flex flex-col gap-2 items-start">
      {board.relayOrder.length > 0 && (
        <div className="flex items-center gap-0.5 text-[11px]">
          <span className="text-purple-300 mr-1 font-semibold">Relay</span>
          {board.relayOrder.map((t, i) => (
            <span key={i} className="flex items-center">
              {i > 0 && <span className="text-gray-600 mx-0.5">→</span>}
              <span
                className={`w-5 h-5 rounded flex items-center justify-center text-[10px] font-bold ${
                  i < board.relayProgress ? 'opacity-30 line-through' : ''
                }`}
                style={{ background: SIGNAL_COLORS[t] ?? '#888', color: '#1a1a1a' }}
                title={i < board.relayProgress ? 'absorbed' : 'pending'}
              >
                {typeToGlyph(t)}
              </span>
            </span>
          ))}
        </div>
      )}
      <div className="rounded-lg p-5 flex gap-3 items-end" style={{ background: BOARD_BG }}>
      {board.lanes.map((lane, i) => {
        const selected = selectedLane === i;
        const isBlind = lane.kind === LaneKind.Blind;
        const topIdx = lane.chips.length - 1;
        return (
          <div key={i} className="flex flex-col items-center gap-1">
            <div className="h-5 text-[10px] leading-none flex items-center gap-1">
              {lane.locked && (
                <span className="text-yellow-400">🔒{typeToGlyph(lane.unlockType)}</span>
              )}
              {lane.pending && <span className="text-orange-400">⏸</span>}
              {lane.kind === LaneKind.Blind && <span className="text-indigo-300">👁</span>}
            </div>
            <button
              onClick={() => onLaneClick(i)}
              className={`flex flex-col-reverse gap-1 p-1 rounded-md transition-shadow ${
                selected ? 'ring-2 ring-white' : ''
              } ${lane.locked ? 'opacity-50' : ''}`}
              style={{ background: SLOT_BG }}
            >
              {Array.from({ length: CAPACITY }).map((_, slot) => {
                const chip = lane.chips[slot];
                if (!chip) {
                  return <div key={slot} className="w-11 h-11 rounded-md" style={{ background: BOARD_BG }} />;
                }
                const hidden = isPlaytest && isBlind && slot !== topIdx;
                return <Chip key={slot} type={chip.type} overload={chip.overload} hidden={hidden} />;
              })}
            </button>
            <div className="h-4 text-[10px] text-gray-500">{i}</div>
          </div>
        );
      })}
      </div>
    </div>
  );
}
