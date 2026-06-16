import { NextResponse } from 'next/server';
import { readChapters, writeChapters } from '../../../lib/csv';

export async function GET() {
  return NextResponse.json(readChapters());
}

export async function POST(request: Request) {
  const body = await request.json();
  const chapters = readChapters();
  const maxId = chapters.reduce((m, c) => Math.max(m, c.chapter_id), 0);
  const newChapter = { ...body, chapter_id: maxId + 1 };
  chapters.push(newChapter);
  writeChapters(chapters);
  return NextResponse.json(newChapter, { status: 201 });
}
