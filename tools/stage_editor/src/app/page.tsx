'use client';

import { useState, useEffect, useCallback } from 'react';
import type { StageRow, ChapterRow, StageMeta, GenerateResult, GeneratorConfig, GenSettings } from '../types/stage';
import {
  BoardState, fromLanes, cloneBoard, applyMove, isCleared, isHardStuck, selectable, solve,
} from '../lib/rules';
import { relayOrderToTypes, parseLaneKinds, parseLockUnlock } from '../lib/signal';
import { encodeBoard, decodeBoard, hasBoard } from '../lib/board-codec';
import ChapterPanel from '../components/ChapterPanel';
import StageList from '../components/StageList';
import BoardView from '../components/BoardView';
import DefinitionPanel from '../components/DefinitionPanel';
import MetadataPanel, { DIFFICULTY_REWARD } from '../components/MetadataPanel';
import GeneratorPanel, { GeneratorStatus } from '../components/GeneratorPanel';
import PlaytestPanel from '../components/PlaytestPanel';

interface PlaytestState {
  board: BoardState;
  selectedLane: number | null;
  moveCount: number;
  cleared: boolean;
  hardStuck: boolean;
}

interface GenInfo { attempts: number; solveLength: number; score: number; }

function defConfig(meta: StageMeta, gen: GenSettings): GeneratorConfig {
  // Generate-definition mode: ignore the per-stage definition, build from the generator block
  // (count-based random gimmicks). Otherwise use the explicit per-stage definition as-is.
  if (gen.useGenerateDef) {
    return {
      types: gen.types,
      laneKinds: 'N'.repeat(Math.max(0, gen.laneCount)), // only length (= total lanes) is read in randomize mode
      lockUnlock: '',
      overloadType: -1,
      relayOrder: '',
      difficulty: gen.difficulty,
      maxAttempts: gen.maxAttempts,
      lockCount: gen.lockCount,
      blindCount: gen.blindCount,
      randomizeGimmicks: true,
      randomOverload: gen.overload,
      randomRelay: gen.relay,
    };
  }
  return {
    types: meta.types,
    laneKinds: meta.lane_kinds,
    lockUnlock: meta.lock_unlock,
    overloadType: meta.overload_type,
    relayOrder: meta.relay_order,
    difficulty: meta.difficulty,
    maxAttempts: gen.maxAttempts,
    lockCount: 0,
    blindCount: 0,
    randomizeGimmicks: false,
    randomOverload: false,
    randomRelay: false,
  };
}

const DEFAULT_GEN_SETTINGS: GenSettings = {
  maxAttempts: 100,
  useGenerateDef: false,
  types: 4,
  laneCount: 6,
  difficulty: 0,
  lockCount: 0,
  blindCount: 0,
  overload: false,
  relay: false,
};

function decodeStageBoard(m: StageMeta): BoardState | null {
  if (!hasBoard(m.board)) return null;
  const kinds = parseLaneKinds(m.lane_kinds);
  const unlock = parseLockUnlock(m.lock_unlock, kinds.length);
  return fromLanes(decodeBoard(m.board, kinds, unlock), m.types, relayOrderToTypes(m.relay_order));
}

export default function EditorPage() {
  const [stages, setStages] = useState<StageRow[]>([]);
  const [chapters, setChapters] = useState<ChapterRow[]>([]);
  const [selectedChapterId, setSelectedChapterId] = useState<number | null>(null);
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [meta, setMeta] = useState<StageMeta | null>(null);
  const [board, setBoard] = useState<BoardState | null>(null);
  const [playtest, setPlaytest] = useState<PlaytestState | null>(null);
  const [simulate, setSimulate] = useState<{ states: BoardState[]; step: number } | null>(null);
  const [dirty, setDirty] = useState(false);
  const [genStatus, setGenStatus] = useState<GeneratorStatus>('idle');
  const [genInfo, setGenInfo] = useState<GenInfo | null>(null);
  const [genError, setGenError] = useState<string | null>(null);
  // Generator settings persist across stage select / New (not stored per-stage).
  const [genSettings, setGenSettings] = useState<GenSettings>(DEFAULT_GEN_SETTINGS);

  useEffect(() => {
    fetch('/api/generator-defaults')
      .then(r => r.json())
      .then(d => setGenSettings(s => ({
        ...s,
        maxAttempts: typeof d.maxAttempts === 'number' ? d.maxAttempts : s.maxAttempts,
        types:       typeof d.types === 'number'       ? d.types       : s.types,
        laneCount:   typeof d.laneCount === 'number'   ? d.laneCount   : s.laneCount,
        difficulty:  typeof d.difficulty === 'number'  ? d.difficulty  : s.difficulty,
      })))
      .catch(() => {});
    fetch('/api/stages').then(r => r.json()).then(setStages).catch(console.error);
    fetch('/api/chapters').then(r => r.json()).then((chs: ChapterRow[]) => {
      setChapters(chs);
      if (chs.length > 0) setSelectedChapterId(chs[0].chapter_id);
    }).catch(console.error);
  }, []);

  const loadStage = useCallback((stage: StageRow) => {
    setSelectedId(stage.stage_id);
    const m: StageMeta = { ...stage };
    setMeta(m);
    setPlaytest(null);
    setSimulate(null);
    setGenStatus('idle');
    setGenInfo(null);
    setGenError(null);
    setDirty(false);
    setBoard(decodeStageBoard(m)); // local decode — no generator call needed to render a saved stage
  }, []);

  const handleSelect = useCallback((id: number) => {
    const stage = stages.find(s => s.stage_id === id);
    if (stage) loadStage(stage);
  }, [stages, loadStage]);

  const newStagePayload = useCallback(async (chapterId: number, order: number) => {
    const d = await fetch('/api/generator-defaults').then(r => r.json()).catch(() => ({}));
    const difficulty = d.difficulty ?? 0;
    const laneCount = d.laneCount ?? 6;
    return {
      chapter_id: chapterId, stage_order: order, difficulty,
      par_moves: 0, // set to optimal solveLength on first Generate
      reward_group_id: DIFFICULTY_REWARD[difficulty] ?? 2001,
      types: d.types ?? 4,
      lane_kinds: 'N'.repeat(laneCount),
      lock_unlock: '', overload_type: -1, relay_order: '', board: '',
    };
  }, []);

  const handleNew = useCallback(async () => {
    const chapterId = selectedChapterId ?? 1;
    const maxOrder = stages.filter(s => s.chapter_id === chapterId).reduce((m, s) => Math.max(m, s.stage_order), 0);
    const payload = await newStagePayload(chapterId, maxOrder + 1);
    const created: StageRow = await fetch('/api/stages', {
      method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload),
    }).then(r => r.json());
    setStages(prev => [...prev, created]);
    loadStage(created);
  }, [selectedChapterId, stages, newStagePayload, loadStage]);

  const handleInsertAfter = useCallback(async (afterOrder: number) => {
    const chapterId = selectedChapterId ?? 1;
    const toShift = stages
      .filter(s => s.chapter_id === chapterId && s.stage_order > afterOrder)
      .sort((a, b) => b.stage_order - a.stage_order);
    const shifted: StageRow[] = [];
    for (const s of toShift) {
      const updated = { ...s, stage_order: s.stage_order + 1 };
      await fetch(`/api/stages/${s.stage_id}`, {
        method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(updated),
      });
      shifted.push(updated);
    }
    const payload = await newStagePayload(chapterId, afterOrder + 1);
    const created: StageRow = await fetch('/api/stages', {
      method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload),
    }).then(r => r.json());
    setStages(prev => {
      const ids = new Set(shifted.map(s => s.stage_id));
      return [...prev.filter(s => !ids.has(s.stage_id)), ...shifted, created];
    });
    loadStage(created);
  }, [selectedChapterId, stages, newStagePayload, loadStage]);

  const handleDelete = useCallback(async (id: number) => {
    await fetch(`/api/stages/${id}`, { method: 'DELETE' });
    setStages(prev => prev.filter(s => s.stage_id !== id));
    if (selectedId === id) { setSelectedId(null); setMeta(null); setBoard(null); setPlaytest(null); setSimulate(null); }
  }, [selectedId]);

  const handleSelectChapter = useCallback((id: number) => {
    setSelectedChapterId(id);
    if (selectedId !== null) {
      const stage = stages.find(s => s.stage_id === selectedId);
      if (stage && stage.chapter_id !== id) {
        setSelectedId(null); setMeta(null); setBoard(null); setPlaytest(null); setSimulate(null);
      }
    }
  }, [selectedId, stages]);

  const handleNewChapter = useCallback(async () => {
    const maxId = chapters.reduce((m, c) => Math.max(m, c.chapter_id), 0);
    const payload = { display_order: maxId + 1, unlock_chapter_id: maxId > 0 ? maxId : null, bg_theme_id: 1 };
    const created: ChapterRow = await fetch('/api/chapters', {
      method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload),
    }).then(r => r.json());
    setChapters(prev => [...prev, created]);
    setSelectedChapterId(created.chapter_id);
  }, [chapters]);

  const handleDeleteChapter = useCallback(async (id: number) => {
    const count = stages.filter(s => s.chapter_id === id).length;
    const msg = count > 0 ? `Chapter ${id} has ${count} stage(s).\nDelete chapter and all its stages?` : `Delete Chapter ${id}?`;
    if (!window.confirm(msg)) return;
    for (const s of stages.filter(s => s.chapter_id === id)) {
      await fetch(`/api/stages/${s.stage_id}`, { method: 'DELETE' });
    }
    await fetch(`/api/chapters/${id}`, { method: 'DELETE' });
    setStages(prev => prev.filter(s => s.chapter_id !== id));
    setChapters(prev => prev.filter(c => c.chapter_id !== id));
    if (selectedChapterId === id) {
      setSelectedChapterId(null); setSelectedId(null); setMeta(null); setBoard(null); setPlaytest(null); setSimulate(null);
    }
  }, [stages, selectedChapterId]);

  // Structural change → the stored board no longer matches; clear it, force regenerate.
  const handleDefChange = useCallback((patch: Partial<StageMeta>) => {
    setMeta(prev => prev ? { ...prev, ...patch, board: '' } : prev);
    setBoard(null);
    setPlaytest(null);
    setSimulate(null);
    setGenInfo(null);
    setGenStatus('idle');
    setDirty(true);
  }, []);

  const handleMetaField = useCallback((key: keyof StageMeta, value: number) => {
    setMeta(prev => {
      if (!prev) return prev;
      if (key === 'difficulty') {
        return { ...prev, difficulty: value, reward_group_id: DIFFICULTY_REWARD[value] ?? prev.reward_group_id };
      }
      return { ...prev, [key]: value };
    });
    setDirty(true);
  }, []);

  const handleGenerate = useCallback(async () => {
    if (!meta) return;
    setGenStatus('running');
    setGenInfo(null);
    setGenError(null);
    setPlaytest(null);
    setSimulate(null);
    try {
      const res = await fetch('/api/generate-board', {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(defConfig(meta, genSettings)),
      });
      const result: GenerateResult | { error: string } | null = await res.json();
      if (!res.ok || !result || 'error' in (result as object)) {
        throw new Error((result as { error?: string })?.error ?? 'Generator failed.');
      }
      const g = result as GenerateResult;
      setBoard(fromLanes(g.lanes, g.types, relayOrderToTypes(g.relayOrder)));
      // Persist the resolved gimmick layout the generator produced (essential in randomize mode,
      // where these were chosen server-side and must match the stored board for correct rendering).
      // In generate-definition mode also adopt the block's types/difficulty so the saved stage matches.
      setMeta(prev => prev ? {
        ...prev,
        board: encodeBoard(g.lanes),
        lane_kinds: g.laneKinds,
        lock_unlock: g.lockUnlock,
        overload_type: g.overloadType,
        relay_order: g.relayOrder,
        par_moves: g.solveLength, // perfect-clear threshold = optimal move count
        ...(genSettings.useGenerateDef ? {
          types: g.types,
          difficulty: genSettings.difficulty,
          reward_group_id: DIFFICULTY_REWARD[genSettings.difficulty] ?? prev.reward_group_id,
        } : {}),
      } : prev);
      setGenInfo({ attempts: g.attempts, solveLength: g.solveLength, score: g.score });
      setGenStatus('success');
      setDirty(true);
    } catch (e) {
      setGenStatus('failed');
      setGenError(e instanceof Error ? e.message : 'Generator failed.');
    }
  }, [meta, genSettings]);

  const handleSave = useCallback(async () => {
    if (!meta || !selectedId) return;
    const row: StageRow = { ...meta };
    await fetch(`/api/stages/${selectedId}`, {
      method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(row),
    });
    setStages(prev => prev.map(s => s.stage_id === selectedId ? row : s));
    setDirty(false);
  }, [meta, selectedId]);

  // ── Playtest ─────────────────────────────────────────────────────────────
  const startPlaytest = useCallback(() => {
    if (!board) return;
    setSimulate(null);
    const b = cloneBoard(board);
    setPlaytest({ board: b, selectedLane: null, moveCount: 0, cleared: isCleared(b), hardStuck: isHardStuck(b) });
  }, [board]);

  const resetPlaytest = useCallback(() => {
    if (!board) return;
    const b = cloneBoard(board);
    setPlaytest({ board: b, selectedLane: null, moveCount: 0, cleared: isCleared(b), hardStuck: isHardStuck(b) });
  }, [board]);

  const handleLaneClick = useCallback((lane: number) => {
    setPlaytest(prev => {
      if (!prev || prev.cleared) return prev;
      if (prev.selectedLane === null) {
        if (!selectable(prev.board, lane)) return prev;
        return { ...prev, selectedLane: lane };
      }
      if (prev.selectedLane === lane) return { ...prev, selectedLane: null };
      const moved = applyMove(prev.board, prev.selectedLane, lane);
      if (!moved) return { ...prev, selectedLane: null };
      return {
        board: moved.board,
        selectedLane: null,
        moveCount: prev.moveCount + 1,
        cleared: isCleared(moved.board),
        hardStuck: isHardStuck(moved.board),
      };
    });
  }, []);

  // ── Simulate (solve the current board in TS, replay step by step) ──────────
  const startSimulate = useCallback(() => {
    if (!board) return;
    const sol = solve(cloneBoard(board));
    if (!sol || sol.length === 0) { setGenError('No solution found within search cap.'); return; }
    let cur = cloneBoard(board);
    const states: BoardState[] = [cloneBoard(cur)];
    for (const [from, to] of sol) {
      const r = applyMove(cur, from, to);
      if (!r) break;
      cur = r.board;
      states.push(cloneBoard(cur));
    }
    setPlaytest(null);
    setSimulate({ states, step: 0 });
  }, [board]);

  const simStep = useCallback((delta: number) => {
    setSimulate(prev => prev
      ? { ...prev, step: Math.max(0, Math.min(prev.states.length - 1, prev.step + delta)) }
      : prev);
  }, []);

  const filteredStages = selectedChapterId !== null
    ? stages.filter(s => s.chapter_id === selectedChapterId)
    : stages;

  const displayBoard = simulate ? simulate.states[simulate.step] : playtest ? playtest.board : board;

  return (
    <div className="flex h-screen overflow-hidden">
      <div className="w-44 flex-shrink-0 border-r border-gray-700 bg-gray-900 flex flex-col">
        <ChapterPanel
          chapters={chapters} stages={stages} selectedChapterId={selectedChapterId}
          onSelect={handleSelectChapter} onNew={handleNewChapter} onDelete={handleDeleteChapter}
        />
        <div className="flex-1 min-h-0 overflow-hidden">
          <StageList
            stages={filteredStages} selectedId={selectedId}
            onSelect={handleSelect} onNew={handleNew} onDelete={handleDelete} onInsertAfter={handleInsertAfter}
          />
        </div>
      </div>

      <div className="flex-1 flex flex-col overflow-hidden">
        {meta ? (
          <>
            <div className="flex-1 flex items-center justify-center overflow-auto p-4">
              <BoardView
                board={displayBoard}
                selectedLane={playtest?.selectedLane ?? null}
                isPlaytest={!!playtest}
                onLaneClick={handleLaneClick}
              />
            </div>
            <div className="flex-shrink-0">
              <MetadataPanel meta={meta} onFieldChange={handleMetaField} />
              <GeneratorPanel
                onGenerate={handleGenerate}
                settings={genSettings} onSettingsChange={setGenSettings}
                status={genStatus} info={genInfo} error={genError}
              />
              <PlaytestPanel
                hasBoard={!!board} hasSeed={hasBoard(meta.board)} isPlaytest={!!playtest}
                moveCount={playtest?.moveCount ?? 0} cleared={playtest?.cleared ?? false}
                hardStuck={playtest?.hardStuck ?? false} dirty={dirty}
                hasSolution={!!board}
                isSimulate={!!simulate}
                simStep={simulate?.step ?? 0}
                simTotal={simulate ? simulate.states.length - 1 : 0}
                onStartPlaytest={startPlaytest} onStopPlaytest={() => setPlaytest(null)}
                onResetPlaytest={resetPlaytest} onSave={handleSave}
                onStartSimulate={startSimulate}
                onStopSimulate={() => setSimulate(null)}
                onSimStep={simStep}
              />
            </div>
          </>
        ) : (
          <div className="flex-1 flex items-center justify-center text-gray-500 text-sm">
            Select or create a stage
          </div>
        )}
      </div>

      <div className="w-56 flex-shrink-0 border-l border-gray-700 bg-gray-900 flex flex-col">
        {meta && <DefinitionPanel meta={meta} onChange={handleDefChange} />}
      </div>
    </div>
  );
}
