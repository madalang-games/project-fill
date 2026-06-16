'use client';

import type { StageMeta } from '../types/stage';
import {
  LaneKind, LANE_KIND_CODE, SIGNAL_GLYPHS, SIGNAL_COLORS, typeToGlyph,
  parseLaneKinds, laneKindsToString, parseLockUnlock, lockUnlockToString,
} from '../lib/signal';

interface Props {
  meta: StageMeta;
  onChange: (patch: Partial<StageMeta>) => void;
}

const KIND_CYCLE: number[] = [LaneKind.Normal, LaneKind.Locked, LaneKind.Blind];
const KIND_COLOR = ['bg-gray-600', 'bg-yellow-700', 'bg-indigo-700'];

export default function DefinitionPanel({ meta, onChange }: Props) {
  const kinds = parseLaneKinds(meta.lane_kinds);
  const unlock = parseLockUnlock(meta.lock_unlock, kinds.length);
  const laneCount = kinds.length;

  const commit = (nextKinds: number[], nextUnlock: number[]) => {
    onChange({
      lane_kinds: laneKindsToString(nextKinds),
      lock_unlock: lockUnlockToString(nextUnlock, nextKinds),
    });
  };

  const setLaneCount = (n: number) => {
    const target = Math.max(1, Math.min(16, n));
    const k = [...kinds];
    const u = [...unlock];
    while (k.length < target) { k.push(LaneKind.Normal); u.push(-1); }
    k.length = target; u.length = target;
    commit(k, u);
  };

  const cycleKind = (i: number) => {
    const cur = KIND_CYCLE.indexOf(kinds[i]);
    const next = KIND_CYCLE[(cur + 1) % KIND_CYCLE.length];
    const k = [...kinds]; k[i] = next;
    const u = [...unlock];
    if (next === LaneKind.Locked && u[i] < 0) u[i] = 0; // default unlock = Red
    if (next !== LaneKind.Locked) u[i] = -1;
    commit(k, u);
  };

  const setUnlock = (i: number, t: number) => {
    const u = [...unlock]; u[i] = t;
    commit(kinds, u);
  };

  const lockedLanes = kinds.filter(k => k === LaneKind.Locked).length;

  return (
    <div className="p-3 flex flex-col gap-3 overflow-y-auto">
      <div className="text-sm font-semibold text-gray-300">Definition</div>

      <Field label="Types (sets)">
        <input
          type="number" min={1} max={10} value={meta.types}
          onChange={e => onChange({ types: Math.max(1, Math.min(10, parseInt(e.target.value) || 1)) })}
          className="w-16 bg-gray-700 border border-gray-600 rounded px-1 py-0.5 text-xs"
        />
        <span className="text-[10px] text-gray-500 ml-1">colors R,B,G…</span>
      </Field>

      <Field label="Lanes">
        <input
          type="number" min={1} max={16} value={laneCount}
          onChange={e => setLaneCount(parseInt(e.target.value) || 1)}
          className="w-16 bg-gray-700 border border-gray-600 rounded px-1 py-0.5 text-xs"
        />
        <span className="text-[10px] text-gray-500 ml-1">≥ types + 1</span>
      </Field>

      <div>
        <div className="text-[11px] text-gray-400 mb-1">Lane kinds (click to cycle N→Lock→Blind)</div>
        <div className="flex flex-wrap gap-1">
          {kinds.map((k, i) => (
            <div key={i} className="flex flex-col items-center gap-0.5">
              <button
                onClick={() => cycleKind(i)}
                className={`w-7 h-7 rounded text-xs font-bold ${KIND_COLOR[k]} hover:brightness-110`}
                title={`Lane ${i}`}
              >
                {LANE_KIND_CODE[k]}
              </button>
              {k === LaneKind.Locked ? (
                <select
                  value={unlock[i] < 0 ? 0 : unlock[i]}
                  onChange={e => setUnlock(i, parseInt(e.target.value))}
                  className="w-7 bg-gray-700 border border-gray-600 rounded text-[10px]"
                  title="Unlock type"
                >
                  {Array.from({ length: meta.types }).map((_, t) => (
                    <option key={t} value={t}>{SIGNAL_GLYPHS[t]}</option>
                  ))}
                </select>
              ) : (
                <span className="text-[9px] text-gray-600">{i}</span>
              )}
            </div>
          ))}
        </div>
        {lockedLanes > 0 && (
          <div className="text-[10px] text-yellow-500 mt-1">{lockedLanes} locked lane(s)</div>
        )}
      </div>

      <Field label="Overload">
        <select
          value={meta.overload_type}
          onChange={e => onChange({ overload_type: parseInt(e.target.value) })}
          className="bg-gray-700 border border-gray-600 rounded px-1 py-0.5 text-xs"
        >
          <option value={-1}>None</option>
          {Array.from({ length: meta.types }).map((_, t) => (
            <option key={t} value={t}>{SIGNAL_GLYPHS[t]}</option>
          ))}
        </select>
      </Field>

      <div>
        <Field label="Relay">
          <input
            type="checkbox"
            checked={!!meta.relay_order}
            onChange={e =>
              onChange({
                relay_order: e.target.checked
                  ? Array.from({ length: meta.types }, (_, t) => typeToGlyph(t)).join('')
                  : '',
              })
            }
          />
          <span className="text-[10px] text-gray-500 ml-1">ordered absorb</span>
        </Field>
        {!!meta.relay_order && (
          <input
            type="text" value={meta.relay_order}
            onChange={e => onChange({ relay_order: e.target.value.toUpperCase().replace(/[^RBGYPCOMLT]/g, '') })}
            className="w-full mt-1 bg-gray-700 border border-gray-600 rounded px-1 py-0.5 text-xs font-mono"
            placeholder="RBGYP"
          />
        )}
      </div>

      {/* preview of the type palette */}
      <div className="flex flex-wrap gap-1 pt-1 border-t border-gray-700">
        {Array.from({ length: meta.types }).map((_, t) => (
          <span
            key={t}
            className="w-5 h-5 rounded flex items-center justify-center text-[10px] font-bold"
            style={{ background: SIGNAL_COLORS[t], color: '#1a1a1a' }}
          >
            {SIGNAL_GLYPHS[t]}
          </span>
        ))}
      </div>
    </div>
  );
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <label className="flex items-center gap-1 text-xs text-gray-300">
      <span className="w-20 flex-shrink-0">{label}</span>
      {children}
    </label>
  );
}
