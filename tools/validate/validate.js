'use strict';
/**
 * Cross-source validator — read-only, never writes generated outputs.
 *   node tools/validate/validate.js          report all problems, exit 1 if any
 *   node tools/validate/validate.js --fix     same report + suggested source edits (advisory only;
 *                                             sources are NEVER auto-edited — enum/FK fixes are
 *                                             intentional decisions, auto-adding would entrench typos)
 *
 * Catches what individual generators miss:
 *   1. enum  — CSV enum-typed cell values must be members of a shared/contracts/GameTypes enum
 *   2. fk    — CSV `FK:<path>` must resolve to a real CSV + single PK, and every value must exist
 *              in the target PK set (referential integrity)
 *   3. schema— a CSV's CS-scope column type must match the same-named column in
 *              server/db/schema.json (only when a CSV basename and a table name overlap)
 */

const fs   = require('fs');
const path = require('path');
const cfg  = require('../config-loader');

const FIX_MODE = process.argv.includes('--fix');

const VALID_PRIMITIVE_TYPES = new Set([
  'int8','int16','int32','int64','uint8','uint16','uint32','uint64',
  'float','double','bool','string',
]);

// ── small CSV reader (header rows 1-4 + data) ──────────────────────────────────
function stripBOM(s) { return s.charCodeAt(0) === 0xFEFF ? s.slice(1) : s; }

function parseCSVLine(line) {
  const fields = [];
  let cur = '', inQuote = false;
  for (let i = 0; i < line.length; i++) {
    const c = line[i];
    if (c === '"') {
      if (inQuote && line[i + 1] === '"') { cur += '"'; i++; }
      else inQuote = !inQuote;
    } else if (c === ',' && !inQuote) { fields.push(cur.trim()); cur = ''; }
    else cur += c;
  }
  fields.push(cur.trim());
  return fields;
}

function readCsv(fullPath) {
  const lines = stripBOM(fs.readFileSync(fullPath, 'utf-8')).split(/\r?\n/).filter(l => l.trim() !== '');
  if (lines.length < 5) return null;
  const names       = parseCSVLine(lines[0]);
  const types       = parseCSVLine(lines[2]);
  const constraints = parseCSVLine(lines[3]);
  const cols = names.map((name, c) => ({
    name,
    type: types[c],
    constraints: constraints[c] ? constraints[c].split(',').map(s => s.trim()).filter(Boolean) : [],
  }));
  const rows = lines.slice(4).map(parseCSVLine);
  return { cols, rows };
}

function collectCSVFiles(dir, base = '') {
  const out = [];
  if (!fs.existsSync(dir)) return out;
  for (const entry of fs.readdirSync(dir)) {
    if (entry.startsWith('_')) continue;
    const full = path.join(dir, entry);
    const rel  = base ? `${base}/${entry}` : entry;
    if (fs.statSync(full).isDirectory()) out.push(...collectCSVFiles(full, rel));
    else if (entry.endsWith('.csv')) out.push({ full, rel: rel.replace(/\.csv$/, '') });
  }
  return out;
}

// ── enum source parsing (shared/contracts/GameTypes/*.cs) ──────────────────────
function parseEnums() {
  const dir = path.join(cfg.root, 'shared', 'contracts', 'GameTypes');
  const enums = new Map(); // name -> Set(memberName)
  if (!fs.existsSync(dir)) return enums;
  for (const file of fs.readdirSync(dir)) {
    if (!file.endsWith('.cs')) continue;
    const src = fs.readFileSync(path.join(dir, file), 'utf-8');
    const re = /enum\s+([A-Za-z_]\w*)\s*\{([^}]*)\}/g;
    let m;
    while ((m = re.exec(src)) !== null) {
      const members = new Set();
      for (const part of m[2].split(',')) {
        const name = part.trim().split('=')[0].trim();
        if (/^[A-Za-z_]\w*$/.test(name)) members.add(name);
      }
      enums.set(m[1], members);
    }
  }
  return enums;
}

function isEnumType(type) {
  return !VALID_PRIMITIVE_TYPES.has(type) && !/^string\(\d+\)$/.test(type);
}

function isIntegerLiteral(v) { return /^-?\d+$/.test(v); }

// ── schema.json (DB column types) ──────────────────────────────────────────────
function parseSchemaColumns() {
  const map = new Map(); // tableName -> Map(colName -> type)
  let schema;
  try { schema = JSON.parse(fs.readFileSync(cfg.paths.dbSchema, 'utf-8')); }
  catch { return map; }
  for (const t of schema.tables || []) {
    const cols = new Map();
    for (const c of t.columns || []) cols.set(c.name, c.type);
    map.set(t.name, cols);
  }
  return map;
}

// ── main ───────────────────────────────────────────────────────────────────────
function main() {
  const csvFiles = collectCSVFiles(cfg.paths.datasDir);
  const enums    = parseEnums();
  const dbTables = parseSchemaColumns();

  // pre-pass: parse every CSV once, index key value sets for FK checks.
  // An unqualified FK targets any PK/UQ column (referential candidate keys).
  const parsed = new Map(); // rel -> { cols, rows, keyValues, keyColNames }
  for (const { full, rel } of csvFiles) {
    const data = readCsv(full);
    if (!data) continue;
    const keyValues = new Set();
    const keyColNames = [];
    data.cols.forEach((c, i) => {
      if (c.constraints.includes('PK') || c.constraints.includes('UQ')) {
        keyColNames.push(c.name);
        for (const row of data.rows) keyValues.add(row[i]);
      }
    });
    parsed.set(rel, { ...data, keyValues, keyColNames });
  }

  const errors = []; // { file, loc, msg, fix? }
  const add = (file, loc, msg, fix) => errors.push({ file, loc, msg, fix });

  for (const [rel, { cols, rows, pkIdx }] of parsed) {
    const file = `shared/datas/${rel}.csv`;

    cols.forEach((col, ci) => {
      // 1. enum membership
      if (isEnumType(col.type)) {
        const members = enums.get(col.type);
        if (!members) {
          add(file, `type "${col.name}"`, `enum type "${col.type}" not found in shared/contracts/GameTypes`,
            `define enum ${col.type} in GameEnums.cs (or fix the type name)`);
        } else {
          rows.forEach((row, ri) => {
            const v = row[ci];
            if (v === '' || v === undefined) return; // nullable empty
            if (members.has(v) || isIntegerLiteral(v)) return;
            add(file, `row ${ri + 5}, "${col.name}"`,
              `value "${v}" is not a member of ${col.type} {${[...members].join(', ')}}`,
              `add "${v}" to enum ${col.type} in GameEnums.cs, or correct the typo`);
          });
        }
      }

      // 2. FK referential integrity. Syntax: FK:<csv-path> targets the PK;
      //    FK:<csv-path>.<column> targets a specific (usually UQ) column.
      const fkCon = col.constraints.find(c => c.startsWith('FK:'));
      if (fkCon) {
        let spec = fkCon.slice(3).replace(/\.csv$/, '');
        let fkCol = null;
        const dot = spec.indexOf('.');
        if (dot >= 0) { fkCol = spec.slice(dot + 1); spec = spec.slice(0, dot); }
        const target = spec;
        const tgt = parsed.get(target);
        if (!tgt) {
          add(file, `constraint "${col.name}"`, `FK target "${target}" — no such CSV (shared/datas/${target}.csv)`,
            `create shared/datas/${target}.csv or correct the FK path`);
        } else {
          let tgtColName, tgtValues;
          if (fkCol) {
            const tci = tgt.cols.findIndex(c => c.name === fkCol);
            if (tci < 0) {
              add(file, `constraint "${col.name}"`, `FK target column "${target}.${fkCol}" not found`,
                `correct the FK column name`);
            } else {
              tgtColName = fkCol;
              tgtValues = new Set(tgt.rows.map(r => r[tci]));
            }
          } else if (tgt.keyValues.size === 0) {
            add(file, `constraint "${col.name}"`, `FK target "${target}" has no PK/UQ key column`,
              `mark a PK or UQ column in shared/datas/${target}.csv, or use FK:${target}.<column>`);
          } else {
            tgtColName = tgt.keyColNames.join('/');
            tgtValues = tgt.keyValues;
          }
          if (tgtValues) rows.forEach((row, ri) => {
            const v = row[ci];
            if (v === '' || v === undefined) return;
            if (!tgtValues.has(v)) {
              add(file, `row ${ri + 5}, "${col.name}"`,
                `FK value "${v}" not found in ${target} "${tgtColName}"`,
                `add a "${v}" row to ${target}.csv, or correct the reference`);
            }
          });
        }
      }

      // 3. CS-scope column type vs DB column type (only when names overlap)
      const basename = rel.split('/').pop();
      if (dbTables.has(basename)) {
        const dbType = dbTables.get(basename).get(col.name);
        if (dbType !== undefined && dbType !== col.type) {
          add(file, `type "${col.name}"`,
            `CSV type "${col.type}" != DB schema.json table "${basename}" column type "${dbType}"`,
            `align the CSV type row with schema.json (or vice versa)`);
        }
      }
    });
  }

  // ── report ─────────────────────────────────────────────────────────────────
  if (errors.length === 0) {
    console.log(`[validate] OK — ${parsed.size} CSV(s), ${enums.size} enum(s), ${dbTables.size} DB table(s); no cross-source problems.`);
    return;
  }

  const byFile = new Map();
  for (const e of errors) {
    if (!byFile.has(e.file)) byFile.set(e.file, []);
    byFile.get(e.file).push(e);
  }
  for (const [file, list] of byFile) {
    console.error(`[validate] ERROR: ${file}`);
    for (const e of list) {
      console.error(`  ${e.loc}: ${e.msg}`);
      if (FIX_MODE && e.fix) console.error(`    fix: ${e.fix}`);
    }
  }
  console.error(`\n[validate] ${errors.length} problem(s) found across ${byFile.size} file(s).`);
  if (!FIX_MODE) console.error('[validate] re-run with --fix for suggested edits (advisory; sources are never auto-edited).');
  process.exit(1);
}

main();
