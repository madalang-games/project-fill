export function parseIni(content: string): Record<string, Record<string, string>> {
  const result: Record<string, Record<string, string>> = {};
  let section = '';

  for (const raw of content.split('\n')) {
    const line = raw.trim();
    if (!line || line.startsWith('#') || line.startsWith(';')) continue;

    const sectionMatch = line.match(/^\[(.+)\]$/);
    if (sectionMatch) {
      section = sectionMatch[1];
      result[section] = {};
      continue;
    }

    const eq = line.indexOf('=');
    if (eq !== -1 && section) {
      const key = line.slice(0, eq).trim();
      const val = line.slice(eq + 1).trim();
      result[section][key] = val;
    }
  }

  return result;
}
