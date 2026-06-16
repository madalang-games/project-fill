'use client';

import type { StageRow } from '../types/stage';
import { hasBoard } from '../lib/board-codec';

const DIFFICULTY_LABELS = ['Easy', 'Normal', 'Hard'];

interface Props {
  stages: StageRow[];
  selectedId: number | null;
  onSelect: (id: number) => void;
  onNew: () => void;
  onDelete: (id: number) => void;
  onInsertAfter: (stageOrder: number) => void;
}

function gimmickLabel(s: StageRow): string {
  const tags: string[] = [];
  if (s.lane_kinds.includes('L')) tags.push('Lock');
  if (s.lane_kinds.includes('B')) tags.push('Blind');
  if (s.relay_order) tags.push('Relay');
  if (s.overload_type >= 0) tags.push('Overload');
  return tags.length ? tags.join('·') : 'Basic';
}

export default function StageList({ stages, selectedId, onSelect, onNew, onDelete, onInsertAfter }: Props) {
  return (
    <div className="flex flex-col h-full">
      <div className="p-2 border-b border-gray-700 flex items-center justify-between flex-shrink-0">
        <span className="text-sm font-semibold text-gray-300">Stages</span>
        <button onClick={onNew} className="text-xs bg-blue-600 hover:bg-blue-500 px-2 py-1 rounded">
          + New
        </button>
      </div>
      <div className="flex-1 overflow-y-auto">
        {stages.length === 0 && <div className="p-3 text-xs text-gray-500">No stages yet</div>}
        {[...stages]
          .sort((a, b) => a.stage_order - b.stage_order)
          .map(s => (
            <div
              key={s.stage_id}
              className={`flex items-center justify-between px-3 py-2 cursor-pointer hover:bg-gray-700 border-b border-gray-800 ${
                selectedId === s.stage_id ? 'bg-gray-700' : ''
              }`}
              onClick={() => onSelect(s.stage_id)}
            >
              <div>
                <div className="text-sm text-white">
                  Stage {s.stage_id} <span className="text-xs text-gray-500">#{s.stage_order}</span>
                </div>
                <div className="text-xs text-gray-400">
                  {DIFFICULTY_LABELS[s.difficulty] ?? '?'} · {s.types}t / {s.lane_kinds.length}L · {gimmickLabel(s)}
                </div>
                {hasBoard(s.board) && <div className="text-xs text-green-500">✓ Generated</div>}
              </div>
              <div className="flex flex-col gap-1 ml-2 flex-shrink-0">
                <button
                  onClick={e => { e.stopPropagation(); onInsertAfter(s.stage_order); }}
                  className="text-gray-600 hover:text-green-400 text-xs leading-none"
                  title="Insert stage after this"
                >
                  ＋
                </button>
                <button
                  onClick={e => { e.stopPropagation(); onDelete(s.stage_id); }}
                  className="text-gray-600 hover:text-red-400 text-xs leading-none"
                >
                  ✕
                </button>
              </div>
            </div>
          ))}
      </div>
    </div>
  );
}
