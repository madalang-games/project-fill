'use client';

interface Props {
  hasBoard: boolean;
  hasSeed: boolean;
  hasSolution: boolean;
  isPlaytest: boolean;
  isSimulate: boolean;
  moveCount: number;
  cleared: boolean;
  hardStuck: boolean;
  dirty: boolean;
  simStep: number;
  simTotal: number;
  onStartPlaytest: () => void;
  onStopPlaytest: () => void;
  onResetPlaytest: () => void;
  onStartSimulate: () => void;
  onStopSimulate: () => void;
  onSimStep: (delta: number) => void;
  onSave: () => void;
}

export default function PlaytestPanel({
  hasBoard, hasSeed, hasSolution, isPlaytest, isSimulate,
  moveCount, cleared, hardStuck, dirty, simStep, simTotal,
  onStartPlaytest, onStopPlaytest, onResetPlaytest,
  onStartSimulate, onStopSimulate, onSimStep, onSave,
}: Props) {
  return (
    <div className="p-3 border-t border-gray-700 bg-gray-800 flex flex-wrap gap-2 items-center">
      {isSimulate ? (
        <>
          <button onClick={onStopSimulate} className="text-xs bg-red-700 hover:bg-red-600 px-3 py-1.5 rounded">
            ■ Stop
          </button>
          <button
            onClick={() => onSimStep(-1)} disabled={simStep === 0}
            className="text-xs bg-gray-600 hover:bg-gray-500 disabled:opacity-40 px-2 py-1.5 rounded"
          >
            ◀
          </button>
          <span className="text-xs text-gray-300 tabular-nums">
            {simStep === 0 ? 'Initial' : `Move ${simStep} / ${simTotal}`}
          </span>
          <button
            onClick={() => onSimStep(1)} disabled={simStep >= simTotal}
            className="text-xs bg-gray-600 hover:bg-gray-500 disabled:opacity-40 px-2 py-1.5 rounded"
          >
            ▶
          </button>
          {simStep >= simTotal && simTotal > 0 && (
            <span className="text-xs text-green-400 font-semibold">★ Solved</span>
          )}
        </>
      ) : !isPlaytest ? (
        <>
          <button
            onClick={onStartPlaytest} disabled={!hasBoard}
            className="text-xs bg-green-700 hover:bg-green-600 disabled:opacity-40 px-3 py-1.5 rounded"
          >
            ▶ Playtest
          </button>
          <button
            onClick={onStartSimulate} disabled={!hasBoard || !hasSolution}
            className="text-xs bg-indigo-700 hover:bg-indigo-600 disabled:opacity-40 px-3 py-1.5 rounded"
            title="Replay the generator's verified solution step by step"
          >
            ⏩ Simulate
          </button>
        </>
      ) : (
        <>
          <button onClick={onStopPlaytest} className="text-xs bg-red-700 hover:bg-red-600 px-3 py-1.5 rounded">
            ■ Stop
          </button>
          <button onClick={onResetPlaytest} className="text-xs bg-gray-600 hover:bg-gray-500 px-3 py-1.5 rounded">
            ↺ Reset
          </button>
          <span className="text-xs text-gray-300 ml-1">Moves: {moveCount}</span>
          {cleared && <span className="text-xs text-green-400 font-semibold">★ Cleared!</span>}
          {hardStuck && !cleared && <span className="text-xs text-red-400 font-semibold">Hard stuck</span>}
        </>
      )}

      <div className="flex-1" />

      <button
        onClick={onSave} disabled={!hasSeed}
        className={`text-xs px-3 py-1.5 rounded disabled:opacity-40 ${
          dirty ? 'bg-blue-700 hover:bg-blue-600' : 'bg-gray-600 hover:bg-gray-500'
        }`}
        title={hasSeed ? 'Persist definition + board to stage.csv' : 'Generate a board first'}
      >
        ⬇ Save{dirty ? ' *' : ''}
      </button>
    </div>
  );
}
