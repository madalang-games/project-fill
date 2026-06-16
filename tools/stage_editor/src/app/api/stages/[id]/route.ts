import { NextResponse } from 'next/server';
import { readStages, writeStages } from '../../../../lib/csv';

type Params = Promise<{ id: string }>;

export async function GET(_req: Request, { params }: { params: Params }) {
  const { id } = await params;
  const stageId = parseInt(id);
  const stage = readStages().find(s => s.stage_id === stageId);
  if (!stage) return NextResponse.json({ error: 'not found' }, { status: 404 });
  return NextResponse.json(stage);
}

export async function PUT(request: Request, { params }: { params: Params }) {
  const { id } = await params;
  const stageId = parseInt(id);
  const body = await request.json();
  const stages = readStages();
  const idx = stages.findIndex(s => s.stage_id === stageId);
  if (idx === -1) return NextResponse.json({ error: 'not found' }, { status: 404 });
  stages[idx] = { ...body, stage_id: stageId };
  writeStages(stages);
  return NextResponse.json(stages[idx]);
}

export async function DELETE(_req: Request, { params }: { params: Params }) {
  const { id } = await params;
  const stageId = parseInt(id);
  const stages = readStages();
  const idx = stages.findIndex(s => s.stage_id === stageId);
  if (idx === -1) return NextResponse.json({ error: 'not found' }, { status: 404 });
  stages.splice(idx, 1);
  writeStages(stages);
  return new NextResponse(null, { status: 204 });
}
