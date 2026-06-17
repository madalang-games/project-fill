'use client';

import type { GenSettings } from '../types/stage';

export type GeneratorStatus = 'idle' | 'running' | 'success' | 'failed';

interface GenInfo {
  attempts: number;
  solveLength: number;
  score: number;
}

interface Props {
  onGenerate: () => void;
  settings: GenSettings;
  onSettingsChange: (next: GenSettings) => void;
  status: GeneratorStatus;
  info: GenInfo | null;
  error: string | null;
}

const DIFFICULTY_LABELS = ['Easy', 'Normal', 'Hard'];

export default function GeneratorPanel({ onGenerate, settings, onSettingsChange, status, info, error }: Props) {
  const patch = (p: Partial<GenSettings>) => onSettingsChange({ ...settings, ...p });
  const num = (v: string, lo: number, hi: number) => Math.max(lo, Math.min(hi, parseInt(v) || lo));

  const statusEl =
    status === 'running' ? <span className="text-yellow-400">Generating…</span>
    : status === 'success' && info ? (
        <span className="text-green-400">
          {info.solveLength} moves · score {Math.round(info.score)} ({info.attempts} tries)
        </span>
      )
    : status === 'failed' ? <span className="text-red-400">{error ?? 'No board found — add lanes or lower types'}</span>
    : null;

  return (
    <div className="p-3 border-t border-gray-700 bg-gray-900 flex flex-col gap-2">
      <div className="flex items-center gap-3 flex-wrap">
        <label className="flex items-center gap-1 text-xs text-gray-400">
          Max attempts
          <input
            type="number" min={1} max={2000} value={settings.maxAttempts}
            onChange={e => patch({ maxAttempts: num(e.target.value, 1, 2000) })}
            className="w-20 bg-gray-700 text-white px-1 py-0.5 rounded"
          />
        </label>
        <label className="flex items-center gap-1 text-xs text-gray-300">
          <input
            type="checkbox" checked={settings.useGenerateDef}
            onChange={e => patch({ useGenerateDef: e.target.checked })}
          />
          Use generate definition
        </label>
        <button
          onClick={onGenerate}
          disabled={status === 'running'}
          className="text-xs bg-purple-700 hover:bg-purple-600 disabled:opacity-50 px-3 py-1.5 rounded"
        >
          {status === 'running' ? 'Generating…' : '⚙ Generate (scored)'}
        </button>
        {statusEl && <div className="text-xs">{statusEl}</div>}
      </div>

      {settings.useGenerateDef && (
        <div className="flex items-center gap-3 flex-wrap text-xs text-gray-400 pl-1 border-l-2 border-purple-700">
          <span className="text-[10px] text-purple-400 pl-1">generate def (right panel ignored) →</span>
          <label className="flex items-center gap-1">
            Types
            <input
              type="number" min={1} max={10} value={settings.types}
              onChange={e => patch({ types: num(e.target.value, 1, 10) })}
              className="w-14 bg-gray-700 text-white px-1 py-0.5 rounded"
            />
          </label>
          <label className="flex items-center gap-1">
            Lanes
            <input
              type="number" min={1} max={16} value={settings.laneCount}
              onChange={e => patch({ laneCount: num(e.target.value, 1, 16) })}
              className="w-14 bg-gray-700 text-white px-1 py-0.5 rounded"
            />
            <span className="text-[10px] text-gray-500">≥ types+1</span>
          </label>
          <label className="flex items-center gap-1">
            Difficulty
            <select
              value={settings.difficulty}
              onChange={e => patch({ difficulty: parseInt(e.target.value) })}
              className="bg-gray-700 text-white px-1 py-0.5 rounded"
            >
              {DIFFICULTY_LABELS.map((d, i) => <option key={i} value={i}>{d}</option>)}
            </select>
          </label>
          <label className="flex items-center gap-1">
            Lock
            <input
              type="number" min={0} max={16} value={settings.lockCount}
              onChange={e => patch({ lockCount: num(e.target.value, 0, 16) })}
              className="w-14 bg-gray-700 text-white px-1 py-0.5 rounded"
            />
          </label>
          <label className="flex items-center gap-1">
            Blind
            <input
              type="number" min={0} max={16} value={settings.blindCount}
              onChange={e => patch({ blindCount: num(e.target.value, 0, 16) })}
              className="w-14 bg-gray-700 text-white px-1 py-0.5 rounded"
            />
          </label>
          <label className="flex items-center gap-1 text-gray-300">
            <input
              type="checkbox" checked={settings.overload}
              onChange={e => patch({ overload: e.target.checked })}
            />
            Overload (random)
          </label>
          <label className="flex items-center gap-1 text-gray-300">
            <input
              type="checkbox" checked={settings.relay}
              onChange={e => patch({ relay: e.target.checked })}
            />
            Relay (random)
          </label>
        </div>
      )}
    </div>
  );
}
