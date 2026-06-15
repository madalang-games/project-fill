'use strict';

const fs = require('fs');
const os = require('os');
const path = require('path');
const crypto = require('crypto');
const childProcess = require('child_process');
const cfg = require('../config-loader');

const TOOL = 'font-subset';
const TARGET_MARKERS = new Set(['C', 'S', 'CS']);

function fail(message) {
  console.error(`[${TOOL}] ERROR: ${message}`);
  process.exit(1);
}

function rel(filePath) {
  return path.relative(cfg.root, filePath).replaceAll(path.sep, '/');
}

function parseArgs(argv) {
  const args = new Set(argv);
  return {
    check: args.has('--check'),
    dryRun: args.has('--dry-run') || args.has('--check'),
    verbose: args.has('--verbose'),
    help: args.has('--help') || args.has('-h'),
  };
}

function printHelp() {
  console.log(`Usage: node tools/subset_tool/subset_fonts.js [--check] [--dry-run] [--verbose]

Reads:
  template.ini [font-subset]
  tools/subset_tool/config.json
  shared/datas/string/*.csv

Writes:
  client/project-fill/Assets/TextMesh Pro/Fonts/*

Options:
  --check    Fail if any target font would change, but do not write.
  --dry-run  Print planned changes without writing.
  --verbose  Print per-font charset and size details.`);
}

function assertInsideRoot(filePath, label) {
  const resolved = path.resolve(filePath);
  if (resolved !== cfg.root && !resolved.startsWith(cfg.root + path.sep)) {
    fail(`${label} must stay inside project root: ${resolved}`);
  }
  return resolved;
}

function loadJson(filePath) {
  try {
    return JSON.parse(fs.readFileSync(filePath, 'utf8'));
  } catch (error) {
    fail(`${rel(filePath)}: ${error.message}`);
  }
}

function validateConfig(config) {
  const errors = [];
  if (!Array.isArray(config.languages) || config.languages.length === 0) {
    errors.push('languages must be a non-empty array');
  }
  if (!Array.isArray(config.fonts) || config.fonts.length === 0) {
    errors.push('fonts must be a non-empty array');
  }

  const languageSet = new Set(config.languages || []);
  for (const [index, font] of (config.fonts || []).entries()) {
    const prefix = `fonts[${index}]`;
    if (!font.source) errors.push(`${prefix}.source is required`);
    if (!font.target) errors.push(`${prefix}.target is required`);
    if (!Array.isArray(font.languages) || font.languages.length === 0) {
      errors.push(`${prefix}.languages must be a non-empty array`);
    } else {
      for (const language of font.languages) {
        if (!languageSet.has(language)) {
          errors.push(`${prefix}.languages contains unknown language "${language}"`);
        }
      }
    }
  }

  if (errors.length > 0) {
    fail(`${rel(cfg.fontSubset.config)}\n  ${errors.join('\n  ')}`);
  }
}

function listCsvFiles(dir) {
  if (!fs.existsSync(dir)) {
    fail(`${rel(dir)}: string data directory not found`);
  }

  return fs.readdirSync(dir)
    .filter(name => name.toLowerCase().endsWith('.csv') && !name.startsWith('_'))
    .map(name => path.join(dir, name))
    .sort();
}

function stripBom(text) {
  return text.charCodeAt(0) === 0xFEFF ? text.slice(1) : text;
}

function parseCsv(content) {
  const rows = [];
  let row = [];
  let current = '';
  let inQuote = false;

  const text = stripBom(content);
  for (let i = 0; i < text.length; i++) {
    const c = text[i];
    if (c === '"') {
      if (inQuote && text[i + 1] === '"') {
        current += '"';
        i++;
      } else {
        inQuote = !inQuote;
      }
    } else if (c === ',' && !inQuote) {
      row.push(current.trim());
      current = '';
    } else if ((c === '\n' || c === '\r') && !inQuote) {
      if (c === '\r' && text[i + 1] === '\n') i++;
      row.push(current.trim());
      current = '';
      if (row.some(value => value !== '')) rows.push(row);
      row = [];
    } else {
      current += c;
    }
  }

  if (current.length > 0 || row.length > 0) {
    row.push(current.trim());
    if (row.some(value => value !== '')) rows.push(row);
  }

  return rows;
}

function isMetadataTargetRow(row) {
  return row.length > 0 && row.every(value => TARGET_MARKERS.has(value));
}

function addText(targetSet, text) {
  for (const char of text || '') {
    targetSet.add(char);
  }
}

function buildLanguageCharsets(config) {
  const charsets = new Map();
  for (const language of config.languages) {
    const set = new Set();
    addText(set, config.commonCharacters || '');
    charsets.set(language, set);
  }

  for (const csvFile of listCsvFiles(cfg.fontSubset.stringDatasDir)) {
    const rows = parseCsv(fs.readFileSync(csvFile, 'utf8'));
    if (rows.length === 0) continue;

    const headers = rows[0];
    const languageColumns = [];
    for (let i = 0; i < headers.length; i++) {
      if (charsets.has(headers[i])) {
        languageColumns.push({ index: i, language: headers[i] });
      }
    }
    if (languageColumns.length === 0) continue;

    const startRow = rows[1] && isMetadataTargetRow(rows[1]) ? 4 : 1;
    for (let r = startRow; r < rows.length; r++) {
      for (const column of languageColumns) {
        addText(charsets.get(column.language), rows[r][column.index] || '');
      }
    }
  }

  return charsets;
}

function collectFontCharset(entry, charsets, config) {
  const result = new Set();
  addText(result, config.commonCharacters || '');
  for (const language of entry.languages) {
    const chars = charsets.get(language);
    if (!chars) fail(`font "${entry.target}" references unknown language "${language}"`);
    for (const char of chars) result.add(char);
  }
  addText(result, entry.extraCharacters || '');
  return Array.from(result).sort().join('');
}

function validateFontFiles(config) {
  const errors = [];
  for (const entry of config.fonts) {
    const sourcePath = assertInsideRoot(path.join(cfg.fontSubset.sourceDir, entry.source), 'source');
    assertInsideRoot(path.join(cfg.fontSubset.targetDir, entry.target), 'target');

    if (!fs.existsSync(sourcePath)) {
      errors.push(`${rel(sourcePath)}: source font not found`);
    }
  }

  if (errors.length > 0) {
    fail(errors.join('\n  '));
  }
}

function sha256(filePath) {
  return crypto.createHash('sha256').update(fs.readFileSync(filePath)).digest('hex');
}

function findPython() {
  const candidates = [
    process.env.FONTTOOLS_PYTHON,
    process.env.PYTHON,
    'python',
    'py',
  ].filter(Boolean);

  for (const candidate of candidates) {
    const result = childProcess.spawnSync(candidate, ['-c', 'import fontTools'], { stdio: 'ignore' });
    if (result.status === 0) return candidate;
  }

  fail('fontTools is missing. Install with: python -m pip install fonttools brotli');
}

function runSubset(python, sourcePath, textPath, outputPath) {
  const args = [
    '-m', 'fontTools.subset', sourcePath,
    `--text-file=${textPath}`,
    `--output-file=${outputPath}`,
    '--layout-features=*',
    '--glyph-names',
    '--symbol-cmap',
    '--legacy-cmap',
    '--notdef-glyph',
    '--notdef-outline',
    '--recommended-glyphs',
    '--name-IDs=*',
    '--name-legacy',
    '--name-languages=*',
    '--ignore-missing-glyphs',
  ];

  const result = childProcess.spawnSync(python, args, {
    encoding: 'utf8',
    stdio: ['ignore', 'pipe', 'pipe'],
  });

  if (result.status !== 0) {
    fail(`${rel(sourcePath)}\n  ${result.stderr || result.stdout || `fontTools exited ${result.status}`}`);
  }
}

function formatBytes(bytes) {
  return `${(bytes / 1024 / 1024).toFixed(2)} MB`;
}

function subsetFont(entry, context) {
  const sourcePath = assertInsideRoot(path.join(cfg.fontSubset.sourceDir, entry.source), 'source');
  const targetPath = assertInsideRoot(path.join(cfg.fontSubset.targetDir, entry.target), 'target');

  const charset = collectFontCharset(entry, context.charsets, context.config);
  const textPath = path.join(context.tempDir, `${path.basename(entry.target)}.chars.txt`);
  const outputPath = path.join(context.tempDir, entry.target);
  fs.mkdirSync(path.dirname(outputPath), { recursive: true });
  fs.writeFileSync(textPath, charset, 'utf8');

  runSubset(context.python, sourcePath, textPath, outputPath);

  const targetExists = fs.existsSync(targetPath);
  const changed = !targetExists || sha256(outputPath) !== sha256(targetPath);
  const sourceBytes = fs.statSync(sourcePath).size;
  const outputBytes = fs.statSync(outputPath).size;
  const targetBytes = targetExists ? fs.statSync(targetPath).size : 0;

  if (context.options.verbose || changed) {
    const action = changed ? (context.options.dryRun ? 'would update' : 'updated') : 'unchanged';
    console.log(`[${TOOL}] ${action}: ${rel(targetPath)} chars=${Array.from(charset).length} source=${formatBytes(sourceBytes)} target=${targetExists ? formatBytes(targetBytes) : '-'} subset=${formatBytes(outputBytes)}`);
  }

  if (changed && !context.options.dryRun) {
    fs.mkdirSync(path.dirname(targetPath), { recursive: true });
    fs.copyFileSync(outputPath, targetPath);
  }

  return { changed, targetPath };
}

function main() {
  const options = parseArgs(process.argv.slice(2));
  if (options.help) {
    printHelp();
    return;
  }

  assertInsideRoot(cfg.fontSubset.config, 'config');
  assertInsideRoot(cfg.fontSubset.sourceDir, 'source_dir');
  assertInsideRoot(cfg.fontSubset.targetDir, 'target_dir');
  assertInsideRoot(cfg.fontSubset.stringDatasDir, 'string_datas_dir');

  const config = loadJson(cfg.fontSubset.config);
  validateConfig(config);
  validateFontFiles(config);

  const tempDir = fs.mkdtempSync(path.join(os.tmpdir(), 'project-fill-font-subset-'));
  const context = {
    options,
    config,
    tempDir,
    python: findPython(),
    charsets: buildLanguageCharsets(config),
  };

  try {
    let changedCount = 0;
    for (const entry of config.fonts) {
      if (subsetFont(entry, context).changed) changedCount++;
    }

    if (options.check && changedCount > 0) {
      fail(`${changedCount} target font(s) are out of date. Run: npm run font:subset`);
    }

    console.log(`[${TOOL}] OK: ${config.fonts.length} font(s), changed=${changedCount}`);
  } finally {
    fs.rmSync(tempDir, { recursive: true, force: true });
  }
}

main();
