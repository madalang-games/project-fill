'use strict';
/**
 * Incremental generator orchestrator.
 *   node tools/gen.js           run only generators whose sources changed since last run
 *   node tools/gen.js --all     run every generator
 *   node tools/gen.js --check   run every generator in --check (drift) mode, no cache writes
 *   node tools/gen.js --watch   watch sources, run affected generators on change
 *
 * Dependency order is fixed: info -> db -> pkt (matches all_generator.bat).
 * Change detection hashes each generator's source tree; the cache lives in
 * tools/.gen-cache/orchestrator.json. Output drift is still caught by `npm run gen:check`.
 */

const fs      = require('fs');
const path    = require('path');
const crypto  = require('crypto');
const { spawnSync } = require('child_process');
const cfg     = require('./config-loader');

const CACHE_FILE = path.join(cfg.root, 'tools', '.gen-cache', 'orchestrator.json');

const GENERATORS = [
  { name: 'info', script: path.join(__dirname, 'info_generator', 'info_generator.js'), sources: [cfg.paths.datasDir] },
  { name: 'db',   script: path.join(__dirname, 'db_generator',   'db_generator.js'),   sources: [cfg.paths.dbSchema] },
  { name: 'pkt',  script: path.join(__dirname, 'pkt_generator',  'pkt_generator.js'),  sources: [path.join(cfg.root, 'shared', 'contracts')] },
];

function hashPath(p, hash) {
  if (!fs.existsSync(p)) return;
  const stat = fs.statSync(p);
  if (stat.isDirectory()) {
    for (const entry of fs.readdirSync(p).sort()) {
      if (entry.startsWith('_')) continue; // mirror generators' skip rule
      hashPath(path.join(p, entry), hash);
    }
  } else {
    hash.update(p);
    hash.update(fs.readFileSync(p));
  }
}

function sourceHash(gen) {
  const hash = crypto.createHash('sha256');
  for (const src of gen.sources) hashPath(src, hash);
  return hash.digest('hex');
}

function loadCache() {
  try { return JSON.parse(fs.readFileSync(CACHE_FILE, 'utf-8')); }
  catch { return {}; }
}

function saveCache(cache) {
  fs.mkdirSync(path.dirname(CACHE_FILE), { recursive: true });
  fs.writeFileSync(CACHE_FILE, JSON.stringify(cache, null, 2) + '\n', 'utf-8');
}

function runGenerator(gen, check) {
  console.log(`[gen] running ${gen.name}${check ? ' --check' : ''}...`);
  const args = check ? [gen.script, '--check'] : [gen.script];
  return spawnSync(process.execPath, args, { stdio: 'inherit' }).status === 0;
}

function runOnce({ all = false, check = false } = {}) {
  const cache = loadCache();
  let ran = 0, failed = 0;

  for (const gen of GENERATORS) {
    const h = sourceHash(gen);
    if (!all && !check && cache[gen.name] === h) {
      console.log(`[gen] ${gen.name}: up to date (sources unchanged)`);
      continue;
    }
    ran++;
    const ok = runGenerator(gen, check);
    if (ok) {
      if (!check) cache[gen.name] = h;
    } else {
      failed++;
      if (!check) break; // stop the pipeline on a write failure; keep going to report all drift
    }
  }

  if (!check) saveCache(cache);
  if (failed > 0) { console.error(`[gen] ${failed} generator(s) failed.`); process.exitCode = 1; return; }
  if (ran === 0) console.log('[gen] nothing to do — all sources unchanged.');
}

function watch() {
  console.log('[gen] watch mode — Ctrl+C to stop.');
  runOnce();
  let timer = null;
  const trigger = () => { clearTimeout(timer); timer = setTimeout(runOnce, 300); };
  for (const gen of GENERATORS) {
    for (const src of gen.sources) {
      if (!fs.existsSync(src)) continue;
      fs.watch(src, { recursive: fs.statSync(src).isDirectory() }, trigger);
    }
  }
}

const args = process.argv.slice(2);
if (args.includes('--watch')) watch();
else runOnce({ all: args.includes('--all'), check: args.includes('--check') });
