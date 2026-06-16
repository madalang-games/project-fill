import { NextResponse } from 'next/server';
import { readChapters, writeChapters } from '../../../../lib/csv';

type Params = Promise<{ id: string }>;

export async function DELETE(_req: Request, { params }: { params: Params }) {
  const { id } = await params;
  const chapterId = parseInt(id);
  const chapters = readChapters();
  const idx = chapters.findIndex(c => c.chapter_id === chapterId);
  if (idx === -1) return NextResponse.json({ error: 'not found' }, { status: 404 });
  chapters.splice(idx, 1);
  writeChapters(chapters);
  return new NextResponse(null, { status: 204 });
}
