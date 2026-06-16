import { NextResponse } from 'next/server';
import * as fs from 'fs';
import * as path from 'path';
import { parseIni } from '../../../lib/ini';

const PROJECT_ROOT = process.env.PROJECT_ROOT ?? path.join(process.cwd(), '..', '..');
const INI_PATH = path.join(PROJECT_ROOT, 'template.ini');

function intVal(raw: string | undefined, fallback: number): number {
  const n = parseInt(raw ?? '');
  return isNaN(n) ? fallback : n;
}

const FALLBACK = {
  types: 4,
  laneCount: 6,
  scrambleSteps: 40,
  maxAttempts: 100,
  difficulty: 0,
};

export async function GET() {
  try {
    const ini = parseIni(fs.readFileSync(INI_PATH, 'utf-8'));
    const s = ini['stage-editor-generator'] ?? {};
    return NextResponse.json({
      types:         intVal(s['types'],          FALLBACK.types),
      laneCount:     intVal(s['lane_count'],      FALLBACK.laneCount),
      scrambleSteps: intVal(s['scramble_steps'],  FALLBACK.scrambleSteps),
      maxAttempts:   intVal(s['max_attempts'],    FALLBACK.maxAttempts),
      difficulty:    intVal(s['difficulty'],      FALLBACK.difficulty),
    });
  } catch {
    return NextResponse.json(FALLBACK);
  }
}
