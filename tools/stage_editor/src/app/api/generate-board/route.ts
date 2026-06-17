import { NextRequest, NextResponse } from 'next/server';
import { execFile } from 'child_process';
import * as fs from 'fs/promises';
import * as os from 'os';
import * as path from 'path';
import { randomUUID } from 'crypto';

export const runtime = 'nodejs';

const PROJECT_ROOT = process.env.PROJECT_ROOT ?? path.join(process.cwd(), '..', '..');
const PUBLISHED_DLL = path.join(PROJECT_ROOT, 'tools', 'stage_generator', 'bin', 'publish', 'StageGenerator.Cli.dll');

function runGenerator(requestPath: string): Promise<string> {
  return new Promise((resolve, reject) => {
    execFile(
      'dotnet',
      ['exec', PUBLISHED_DLL, requestPath],
      // timeout > the CLI's own time budget so the CLI returns best-so-far gracefully;
      // this is the hard kill if it ever ignores its own budget.
      { cwd: PROJECT_ROOT, windowsHide: true, maxBuffer: 1024 * 1024 * 32, timeout: 30_000, killSignal: 'SIGKILL' },
      (error, stdout, stderr) => {
        if (error) {
          reject(new Error(stderr || error.message));
          return;
        }
        resolve(stdout);
      },
    );
  });
}

// Forwards the editor's generator config to the C# stage_generator CLI.
// Body: { types, laneKinds, lockUnlock, overloadType, relayOrder, scrambleSteps,
//         difficulty, maxAttempts, seed?, reproduce? }
export async function POST(request: NextRequest) {
  const body = await request.json();
  const payload = {
    types:             body.types ?? 0,
    laneKinds:         body.laneKinds ?? '',
    lockUnlock:        body.lockUnlock ?? '',
    overloadType:      body.overloadType ?? -1,
    relayOrder:        body.relayOrder ?? '',
    scrambleSteps:     body.scrambleSteps ?? 0,
    difficulty:        body.difficulty ?? 0,
    seed:              body.seed ?? 0,
    reproduce:         body.reproduce ?? false,
    maxAttempts:       body.maxAttempts ?? 100,
    // Randomize mode (count-based gimmick placement).
    lockCount:         body.lockCount ?? 0,
    blindCount:        body.blindCount ?? 0,
    randomizeGimmicks: body.randomizeGimmicks ?? false,
    randomOverload:    body.randomOverload ?? false,
    randomRelay:       body.randomRelay ?? false,
  };
  const requestPath = path.join(os.tmpdir(), `project-fill-stage-gen-${randomUUID()}.json`);

  try {
    await fs.writeFile(requestPath, JSON.stringify(payload), 'utf-8');
    const stdout = await runGenerator(requestPath);
    const text = stdout.trim();
    return NextResponse.json(text ? JSON.parse(text) : null);
  } catch (error) {
    const message = error instanceof Error ? error.message : 'Generator failed.';
    return NextResponse.json({ error: message }, { status: 500 });
  } finally {
    await fs.rm(requestPath, { force: true }).catch(() => {});
  }
}
