import * as fs from 'fs';
import * as path from 'path';
import type { StageRow, ChapterRow } from '../types/stage';

const PROJECT_ROOT = process.env.PROJECT_ROOT ?? path.join(process.cwd(), '..', '..');
const STAGE_CSV = path.join(PROJECT_ROOT, 'shared', 'datas', 'stage', 'stage.csv');
const CHAPTER_CSV = path.join(PROJECT_ROOT, 'shared', 'datas', 'stage', 'chapter.csv');

// project-fill CSVs use a 4-line header: names, CS row, type row, constraint row.
const HEADER_LINES = 4;

function parseCSVLine(line: string): string[] {
  const fields: string[] = [];
  let i = 0;
  while (i <= line.length) {
    if (i === line.length) { fields.push(''); break; }
    if (line[i] === '"') {
      let field = '';
      i++;
      while (i < line.length) {
        if (line[i] === '"' && line[i + 1] === '"') { field += '"'; i += 2; }
        else if (line[i] === '"') { i++; break; }
        else { field += line[i++]; }
      }
      if (i < line.length && line[i] === ',') i++;
      fields.push(field);
    } else {
      const end = line.indexOf(',', i);
      if (end === -1) { fields.push(line.slice(i)); break; }
      fields.push(line.slice(i, end));
      i = end + 1;
    }
  }
  return fields;
}

function serializeField(f: string): string {
  if (f.includes(',') || f.includes('"') || f.includes('\n')) {
    return '"' + f.replace(/"/g, '""') + '"';
  }
  return f;
}

function serializeCSVLine(fields: string[]): string {
  return fields.map(serializeField).join(',');
}

function readCSV(csvPath: string): { headers: string[]; data: string[][] } {
  let content = fs.readFileSync(csvPath, 'utf-8');
  if (content.charCodeAt(0) === 0xfeff) content = content.slice(1); // strip BOM
  const lines = content.split('\n').map(l => l.trimEnd()).filter(l => l.length > 0);
  const headers = lines.slice(0, HEADER_LINES);
  const data = lines.slice(HEADER_LINES).map(parseCSVLine);
  return { headers, data };
}

function writeCSV(csvPath: string, dataLines: string[]): void {
  const { headers } = readCSV(csvPath);
  fs.writeFileSync(csvPath, [...headers, ...dataLines].join('\n') + '\n', 'utf-8');
}

// ── stage.csv ──────────────────────────────────────────────────────────────

function rowToStage(f: string[]): StageRow {
  return {
    stage_id:        parseInt(f[0]),
    chapter_id:      parseInt(f[1]) || 1,
    stage_order:     parseInt(f[2]) || 1,
    difficulty:      parseInt(f[3]) || 0,
    reward_group_id: parseInt(f[4]) || 0,
    types:           parseInt(f[5]) || 0,
    lane_kinds:      f[6] ?? '',
    lock_unlock:     f[7] ?? '',
    overload_type:   f[8] === '' || f[8] == null ? -1 : parseInt(f[8]),
    relay_order:     f[9] ?? '',
    board:           f[10] ?? '',
  };
}

function stageToRow(s: StageRow): string[] {
  return [
    String(s.stage_id),
    String(s.chapter_id ?? 1),
    String(s.stage_order ?? 1),
    String(s.difficulty ?? 0),
    String(s.reward_group_id ?? 0),
    String(s.types ?? 0),
    s.lane_kinds ?? '',
    s.lock_unlock ?? '',
    String(s.overload_type ?? -1),
    s.relay_order ?? '',
    s.board ?? '',
  ];
}

export function readStages(): StageRow[] {
  return readCSV(STAGE_CSV).data.map(rowToStage);
}

export function writeStages(stages: StageRow[]): void {
  writeCSV(STAGE_CSV, stages.map(s => serializeCSVLine(stageToRow(s))));
}

// ── chapter.csv ──────────────────────────────────────────────────────────────

function rowToChapter(f: string[]): ChapterRow {
  return {
    chapter_id:        parseInt(f[0]),
    display_order:     parseInt(f[1]) || 1,
    unlock_chapter_id: f[2] ? parseInt(f[2]) : null,
    bg_theme_id:       parseInt(f[3]) || 1,
  };
}

function chapterToRow(c: ChapterRow): string[] {
  return [
    String(c.chapter_id),
    String(c.display_order),
    c.unlock_chapter_id != null ? String(c.unlock_chapter_id) : '',
    String(c.bg_theme_id),
  ];
}

export function readChapters(): ChapterRow[] {
  return readCSV(CHAPTER_CSV).data.map(rowToChapter);
}

export function writeChapters(chapters: ChapterRow[]): void {
  writeCSV(CHAPTER_CSV, chapters.map(c => serializeCSVLine(chapterToRow(c))));
}
