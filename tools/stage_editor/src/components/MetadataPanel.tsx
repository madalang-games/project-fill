'use client';

import type { StageMeta } from '../types/stage';

export const DIFFICULTY_LABELS = ['Easy', 'Normal', 'Hard'];
export const DIFFICULTY_REWARD: Record<number, number> = { 0: 2001, 1: 2002, 2: 2003 };

interface Props {
  meta: StageMeta;
  onFieldChange: (key: keyof StageMeta, value: number) => void;
}

function NumField({ label, value, min, max, onChange }: {
  label: string; value: number; min: number; max: number; onChange: (v: number) => void;
}) {
  return (
    <label className="flex items-center gap-1 text-xs text-gray-300">
      <span className="flex-shrink-0">{label}</span>
      <input
        type="number" value={value} min={min} max={max}
        onChange={e => onChange(parseInt(e.target.value))}
        className="w-16 bg-gray-700 border border-gray-600 rounded px-1 py-0.5 text-white text-xs"
      />
    </label>
  );
}

export default function MetadataPanel({ meta, onFieldChange }: Props) {
  return (
    <div className="p-3 border-t border-gray-700 bg-gray-800 flex flex-wrap gap-3 items-center">
      <NumField label="Order" value={meta.stage_order} min={1} max={999}
        onChange={v => onFieldChange('stage_order', Math.round(v))} />
      <label className="flex items-center gap-1 text-xs text-gray-300">
        <span>Difficulty</span>
        <select
          value={meta.difficulty}
          onChange={e => onFieldChange('difficulty', parseInt(e.target.value))}
          className="bg-gray-700 border border-gray-600 rounded px-1 py-0.5 text-white text-xs"
        >
          {DIFFICULTY_LABELS.map((l, i) => <option key={i} value={i}>{l}</option>)}
        </select>
        <span className="text-gray-500 text-xs ml-1">→ {meta.reward_group_id}</span>
      </label>
      <span className="text-xs text-gray-400">
        board: <span className={meta.board ? 'text-green-400' : 'text-gray-600'}>
          {meta.board ? `${meta.board.length} ch` : 'none'}
        </span>
      </span>
    </div>
  );
}
