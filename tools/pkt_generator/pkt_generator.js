'use strict';

// sync-contracts: shared/contracts/**/*.cs -> client/.../Generated/Contracts/*.cs
// Flat copy; only overwrites if content differs (preserves Unity .meta, avoids git noise).

const fs   = require('fs');
const path = require('path');
const cfg  = require('../config-loader');

const SOURCE_DIR = path.join(cfg.root, 'shared', 'contracts');
const TARGET_DIR = cfg.packetGen.clientOutputDir;
const CHECK_ONLY = process.argv.includes('--check');

function ensureDir(dir) {
  if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });
}

const EXCLUDED_DIRS = new Set(['bin', 'obj', 'auth']);

function collectCSFiles(dir, fileMap = {}) {
  const entries = fs.readdirSync(dir, { withFileTypes: true });
  for (const entry of entries) {
    if (entry.name.startsWith('_')) continue;
    const fullPath = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      if (EXCLUDED_DIRS.has(entry.name)) continue;
      collectCSFiles(fullPath, fileMap);
    } else if (entry.name.endsWith('.cs')) {
      if (fileMap[entry.name]) {
        console.error(`[sync-contracts] ERROR: duplicate filename "${entry.name}"\n  ${fullPath}\n  ${fileMap[entry.name]}`);
        process.exit(1);
      }
      fileMap[entry.name] = fullPath;
    }
  }
  return fileMap;
}

function main() {
  console.log('[sync-contracts] Starting synchronization...');

  if (!fs.existsSync(SOURCE_DIR)) {
    console.error(`[sync-contracts] Source directory not found: ${SOURCE_DIR}`);
    process.exit(1);
  }

  if (!CHECK_ONLY) ensureDir(TARGET_DIR);

  const sourceMap = collectCSFiles(SOURCE_DIR);
  let updatedCount = 0, skippedCount = 0, deletedCount = 0;

  // Copy new/changed files
  for (const [fileName, sourcePath] of Object.entries(sourceMap)) {
    const targetPath = path.join(TARGET_DIR, fileName);
    const sourceContent = fs.readFileSync(sourcePath, 'utf-8');

    if (fs.existsSync(targetPath)) {
      const targetContent = fs.readFileSync(targetPath, 'utf-8');
      if (sourceContent === targetContent) {
        skippedCount++;
        continue;
      }
    }

    if (!CHECK_ONLY) {
      fs.writeFileSync(targetPath, sourceContent, 'utf-8');
      console.log(`[sync-contracts] Updated: ${fileName}`);
    }
    updatedCount++;
  }

  // Remove deleted files
  if (fs.existsSync(TARGET_DIR)) {
    for (const fileName of fs.readdirSync(TARGET_DIR)) {
      if (!fileName.endsWith('.cs')) continue;
      if (!sourceMap[fileName]) {
        if (!CHECK_ONLY) {
          fs.unlinkSync(path.join(TARGET_DIR, fileName));
          console.log(`[sync-contracts] Deleted: ${fileName}`);
        }
        deletedCount++;
      }
    }
  }

  if (CHECK_ONLY && (updatedCount > 0 || deletedCount > 0)) {
    console.error(`[sync-contracts] ERROR: generated contracts are out of date (updated=${updatedCount} deleted=${deletedCount}).`);
    process.exit(1);
  }

  console.log(`[sync-contracts] Done — updated=${updatedCount} skipped=${skippedCount} deleted=${deletedCount}`);
}

main();
