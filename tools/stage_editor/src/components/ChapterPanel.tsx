'use client';

import type { ChapterRow, StageRow } from '../types/stage';

interface Props {
  chapters: ChapterRow[];
  stages: StageRow[];
  selectedChapterId: number | null;
  onSelect: (id: number) => void;
  onNew: () => void;
  onDelete: (id: number) => void;
}

export default function ChapterPanel({ chapters, stages, selectedChapterId, onSelect, onNew, onDelete }: Props) {
  const sorted = [...chapters].sort((a, b) => a.chapter_id - b.chapter_id);

  return (
    <div className="flex flex-col flex-shrink-0 border-b border-gray-700">
      <div className="p-2 flex items-center justify-between">
        <span className="text-sm font-semibold text-gray-300">Chapters</span>
        <button onClick={onNew} className="text-xs bg-blue-600 hover:bg-blue-500 px-2 py-1 rounded">
          + New
        </button>
      </div>
      <div className="max-h-40 overflow-y-auto">
        {sorted.length === 0 && <div className="px-3 py-2 text-xs text-gray-500">No chapters</div>}
        {sorted.map(ch => {
          const count = stages.filter(s => s.chapter_id === ch.chapter_id).length;
          const isSelected = selectedChapterId === ch.chapter_id;
          return (
            <div
              key={ch.chapter_id}
              className={`flex items-center justify-between px-3 py-1.5 cursor-pointer hover:bg-gray-700 border-b border-gray-800 ${isSelected ? 'bg-gray-700' : ''}`}
              onClick={() => onSelect(ch.chapter_id)}
            >
              <span className={`text-sm ${isSelected ? 'text-white' : 'text-gray-300'}`}>Ch {ch.chapter_id}</span>
              <span className="text-xs text-gray-500 mx-2">{count}</span>
              <button
                onClick={e => { e.stopPropagation(); onDelete(ch.chapter_id); }}
                className="text-gray-600 hover:text-red-400 text-sm flex-shrink-0"
              >
                ✕
              </button>
            </div>
          );
        })}
      </div>
    </div>
  );
}
