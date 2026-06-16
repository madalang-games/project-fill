'use client';

import { useState, useEffect } from 'react';

export type GeneratorStatus = 'idle' | 'running' | 'success' | 'failed';

interface GenInfo {
  attempts: number;
  solveLength: number;
  score: number;
}

interface Props {
  onGenerate: (maxAttempts: number) => void;
  status: GeneratorStatus;
  info: GenInfo | null;
  error: string | null;
}

export default function GeneratorPanel({ onGenerate, status, info, error }: Props) {
  const [maxAttempts, setMaxAttempts] = useState(100);

  useEffect(() => {
    fetch('/api/generator-defaults')
      .then(r => r.json())
      .then(d => { if (typeof d.maxAttempts === 'number') setMaxAttempts(d.maxAttempts); })
      .catch(() => {});
  }, []);

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
    <div className="p-3 border-t border-gray-700 bg-gray-900 flex items-center gap-3">
      <label className="flex items-center gap-1 text-xs text-gray-400">
        Max attempts
        <input
          type="number" min={1} max={2000} value={maxAttempts}
          onChange={e => setMaxAttempts(Math.min(2000, Math.max(1, parseInt(e.target.value) || 1)))}
          className="w-20 bg-gray-700 text-white px-1 py-0.5 rounded"
        />
      </label>
      <button
        onClick={() => onGenerate(maxAttempts)}
        disabled={status === 'running'}
        className="text-xs bg-purple-700 hover:bg-purple-600 disabled:opacity-50 px-3 py-1.5 rounded"
      >
        {status === 'running' ? 'Generating…' : '⚙ Generate (scored)'}
      </button>
      {statusEl && <div className="text-xs">{statusEl}</div>}
    </div>
  );
}
