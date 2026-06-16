import { NextResponse } from 'next/server';
import { readStages, writeStages } from '../../../lib/csv';

export async function GET() {
  return NextResponse.json(readStages());
}

export async function POST(request: Request) {
  const body = await request.json();
  const stages = readStages();
  const maxId = stages.reduce((m, s) => Math.max(m, s.stage_id), 0);
  const newStage = { ...body, stage_id: maxId + 1 };
  stages.push(newStage);
  writeStages(stages);
  return NextResponse.json(newStage, { status: 201 });
}
