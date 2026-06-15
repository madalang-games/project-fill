'use strict';

const fs   = require('fs');
const path = require('path');
const cfg  = require('../config-loader');

// ── CLI flags ──────────────────────────────────────────────────────────────────
const GENERATE_ONLY = process.argv.includes('--generate-only');
const ALLOW_DROPS   = process.argv.includes('--allow-drops');
const CHECK_ONLY    = process.argv.includes('--check');

// ── Type maps ──────────────────────────────────────────────────────────────────

const MYSQL_TYPES = {
  int8:     'TINYINT',
  int16:    'SMALLINT',
  int32:    'INT',
  int64:    'BIGINT',
  uint8:    'TINYINT UNSIGNED',
  uint16:   'SMALLINT UNSIGNED',
  uint32:   'INT UNSIGNED',
  uint64:   'BIGINT UNSIGNED',
  float:    'FLOAT',
  double:   'DOUBLE',
  bool:     'TINYINT(1)',
  string:   'TEXT',
  longtext: 'LONGTEXT',
  datetime: 'DATETIME(6)',
  date:     'DATE',
  json:     'JSON',
  bytes:    'BLOB',
  uuid:     'CHAR(36)',
};

const CSHARP_TYPES = {
  int8:     'sbyte',
  int16:    'short',
  int32:    'int',
  int64:    'long',
  uint8:    'byte',
  uint16:   'ushort',
  uint32:   'uint',
  uint64:   'ulong',
  float:    'float',
  double:   'double',
  bool:     'bool',
  string:   'string',
  longtext: 'string',
  datetime: 'DateTimeOffset',
  date:     'DateOnly',
  json:     'string',
  bytes:    'byte[]',
  uuid:     'Guid',
};

// Types that Pomelo maps from C# primitive without explicit HasColumnType
const POMELO_NATIVE = new Set(['int32', 'int64', 'float', 'double', 'bool']);

// ── Naming helpers ─────────────────────────────────────────────────────────────

function toPascal(s) {
  return s.split('_').map(w => w.charAt(0).toUpperCase() + w.slice(1)).join('');
}

function toCamel(s) {
  const parts = s.split('_');
  return parts[0] + parts.slice(1).map(w => w.charAt(0).toUpperCase() + w.slice(1)).join('');
}

function singularPascal(tableName) {
  const parts = tableName.split('_');
  let last = parts[parts.length - 1];
  if (last.endsWith('ies')) last = `${last.slice(0, -3)}y`;
  else if (!last.endsWith('ss') && !last.endsWith('us') && last.endsWith('s')) last = last.slice(0, -1);
  parts[parts.length - 1] = last;
  return toPascal(parts.join('_'));
}

function baseType(type) {
  return type.replace(/\(.+\)$/, '');
}

// ── Type conversion ────────────────────────────────────────────────────────────

function getMysqlType(type) {
  const varcharMatch = type.match(/^string\((\d+)\)$/);
  if (varcharMatch) return `VARCHAR(${varcharMatch[1]})`;
  const bt = baseType(type);
  if (!MYSQL_TYPES[bt]) die(`Unknown schema type: ${type}`);
  return MYSQL_TYPES[bt];
}

function getCsharpBaseType(type) {
  const bt = baseType(type);
  if (!CSHARP_TYPES[bt]) die(`Unknown schema type: ${type}`);
  return CSHARP_TYPES[bt];
}

function getCsharpType(type, nullable) {
  const base = getCsharpBaseType(type);
  return nullable ? `${base}?` : base;
}

function isColNullable(col) {
  // pk implies NOT NULL; explicit null:false → NOT NULL; otherwise nullable
  return !col.pk && col.null !== false;
}

function getInsertExpr(col) {
  const prop = toPascal(col.name);
  const bt   = baseType(col.type);
  const nullable = isColNullable(col);
  if (bt === 'datetime') return nullable ? `{row.${prop}?.UtcDateTime}` : `{row.${prop}.UtcDateTime}`;
  if (bt === 'bool') {
    return nullable
      ? `{(row.${prop}.HasValue ? (row.${prop}.Value ? 1 : 0) : (object)System.DBNull.Value)}`
      : `{(row.${prop} ? 1 : 0)}`;
  }
  return `{row.${prop}}`;
}

// ── Default value helpers ──────────────────────────────────────────────────────

function efDefaultValue(col) {
  if (col.default === undefined) return null;
  const v = col.default;
  if (typeof v === 'boolean') return String(v);     // true / false
  if (typeof v === 'string')  return `"${v}"`;      // "NONE"
  return String(v);                                  // 0, 1, etc.
}

function sqlDefaultClause(col) {
  if (col.default === undefined) return '';
  const v = col.default;
  if (typeof v === 'boolean') return ` DEFAULT ${v ? 1 : 0}`;
  if (typeof v === 'string')  return ` DEFAULT '${v}'`;
  return ` DEFAULT ${v}`;
}

// ── Schema loading ─────────────────────────────────────────────────────────────

function loadSchema() {
  const raw = fs.readFileSync(cfg.paths.dbSchema, 'utf-8');
  const schema = JSON.parse(raw);
  if (!schema.output)             die('schema.json: missing "output" section');
  if (!schema.output.namespace)   die('schema.json: missing "output.namespace"');
  if (!schema.output.context)     die('schema.json: missing "output.context"');
  if (!Array.isArray(schema.tables) || !schema.tables.length)
    die('schema.json: "tables" must be a non-empty array');
  return schema;
}

function collectSchemaErrors(schema) {
  const errors = [];
  const nameRe = /^[a-z][a-z0-9_]*$/;
  const tableNames = new Set();
  const graph = new Map();

  for (const table of schema.tables) {
    if (!nameRe.test(table.name)) { errors.push(`Table "${table.name}": invalid name`); continue; }
    if (tableNames.has(table.name)) { errors.push(`Duplicate table: "${table.name}"`); continue; }
    tableNames.add(table.name);
    graph.set(table.name, new Set());

    if (!Array.isArray(table.columns) || !table.columns.length) {
      errors.push(`"${table.name}": columns must be a non-empty array`);
      continue;
    }

    const colNames = new Set();
    const pkCols = [];
    let hasAuto = false;

    for (const col of table.columns) {
      if (!nameRe.test(col.name)) errors.push(`${table.name}.${col.name}: invalid column name`);
      if (colNames.has(col.name)) errors.push(`${table.name}: duplicate column "${col.name}"`);
      colNames.add(col.name);
      if (col.pk) pkCols.push(col);
      if (col.auto) {
        hasAuto = true;
        if (!col.pk) errors.push(`${table.name}.${col.name}: auto requires pk:true`);
      }
    }

    if (pkCols.length === 0)          errors.push(`"${table.name}": no PK column defined`);
    if (pkCols.length > 1 && hasAuto) errors.push(`"${table.name}": composite PK cannot have auto:true`);
    if (table.conflict !== undefined && table.conflict !== 'ignore')
      errors.push(`"${table.name}": "conflict" must be "ignore" if specified`);

    const idxNames = new Set();
    for (const idx of (table.indexes || [])) {
      if (!nameRe.test(idx.name)) errors.push(`${table.name}.${idx.name}: invalid index name`);
      if (idxNames.has(idx.name)) errors.push(`${table.name}: duplicate index "${idx.name}"`);
      idxNames.add(idx.name);
      if (!Array.isArray(idx.columns) || !idx.columns.length) {
        errors.push(`${table.name}.${idx.name}: index columns must be a non-empty array`);
        continue;
      }
      for (const idxCol of idx.columns) {
        if (!colNames.has(idxCol)) errors.push(`${table.name}.${idx.name}: unknown column "${idxCol}"`);
      }
    }
  }

  for (const table of schema.tables) {
    for (const col of table.columns) {
      if (!col.fk) continue;
      const parts = col.fk.split('.');
      if (parts.length !== 2) { errors.push(`${table.name}.${col.name}: fk must be "table.column"`); continue; }
      const [refTable, refCol] = parts;
      const rt = schema.tables.find(t => t.name === refTable);
      if (!rt) errors.push(`${table.name}.${col.name}: fk references unknown table "${refTable}"`);
      else {
        graph.get(table.name)?.add(refTable);
        if (!rt.columns.find(c => c.name === refCol))
          errors.push(`${table.name}.${col.name}: fk references unknown column "${refTable}.${refCol}"`);
      }
    }
  }

  const visiting = new Set();
  const visited = new Set();
  const stack = [];

  function visit(tableName) {
    if (visiting.has(tableName)) {
      const cycleStart = stack.indexOf(tableName);
      const cycle = [...stack.slice(cycleStart), tableName].join(' -> ');
      errors.push(`Circular FK reference detected: ${cycle}`);
      return;
    }
    if (visited.has(tableName)) return;

    visiting.add(tableName);
    stack.push(tableName);
    for (const next of graph.get(tableName) || []) visit(next);
    stack.pop();
    visiting.delete(tableName);
    visited.add(tableName);
  }

  for (const tableName of graph.keys()) visit(tableName);

  return errors;
}

function validateSchema(schema) {
  const errors = collectSchemaErrors(schema);
  if (errors.length) {
    console.error('[db-generator] Schema validation failed:');
    errors.forEach(e => console.error(`  - ${e}`));
    process.exit(1);
  }
}

function tableByName(schema, name) {
  return schema.tables.find(t => t.name === name);
}

function columnByName(table, name) {
  return table.columns.find(c => c.name === name);
}

function fkColumns(table) {
  return table.columns
    .filter(c => c.fk)
    .map(c => {
      const [refTableName, refColumnName] = c.fk.split('.');
      return { column: c, refTableName, refColumnName };
    });
}

function referencingFks(table, schema) {
  const result = [];
  for (const owner of schema.tables) {
    for (const fk of fkColumns(owner)) {
      if (fk.refTableName === table.name) result.push({ owner, ...fk });
    }
  }
  return result;
}

// ── SQL generation ─────────────────────────────────────────────────────────────

function buildCreateTable(table) {
  const defs  = [];
  const pkCols = table.columns.filter(c => c.pk);

  for (const col of table.columns) {
    const mysqlType = getMysqlType(col.type);
    const notNull   = (!isColNullable(col)) ? ' NOT NULL' : ' NULL';
    const autoInc   = col.auto ? ' AUTO_INCREMENT' : '';
    const dflt      = sqlDefaultClause(col);
    defs.push(`  \`${col.name}\` ${mysqlType}${notNull}${autoInc}${dflt}`);
  }

  defs.push(`  PRIMARY KEY (${pkCols.map(c => `\`${c.name}\``).join(', ')})`);

  for (const col of table.columns) {
    if (col.unique) defs.push(`  UNIQUE KEY \`ux_${table.name}_${col.name}\` (\`${col.name}\`)`);
  }

  for (const idx of (table.indexes || [])) {
    const uk   = idx.unique ? 'UNIQUE KEY' : 'KEY';
    const cols = idx.columns.map(c => `\`${c}\``).join(', ');
    defs.push(`  ${uk} \`${idx.name}\` (${cols})`);
  }

  for (const col of table.columns) {
    if (!col.fk) continue;
    const [refTable, refCol] = col.fk.split('.');
    defs.push(`  CONSTRAINT \`fk_${table.name}_${col.name}\` FOREIGN KEY (\`${col.name}\`) REFERENCES \`${refTable}\` (\`${refCol}\`)`);
  }

  const commentClause = table.comment
    ? ` COMMENT='${table.comment.replace(/'/g, "\\'")}'`
    : '';

  return (
    `CREATE TABLE IF NOT EXISTS \`${table.name}\` (\n` +
    defs.join(',\n') +
    `\n) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci${commentClause};`
  );
}

function buildMigrationSql(schema, dbState) {
  // dbState=null → generate-only: emit CREATE TABLE IF NOT EXISTS for all tables
  // dbState=Map<tableName, {columns:Set<name>, indexes:Set<name>}>
  const ts = new Date().toISOString();
  const lines = [`-- Generated by db-generator | DB: mysql | ${ts}`, ''];
  let hasChanges = false;

  if (!dbState) {
    for (const table of schema.tables) { lines.push(buildCreateTable(table), ''); }
    return lines.join('\n');
  }

  const schemaTables = new Set(schema.tables.map(t => t.name));

  for (const table of schema.tables) {
    if (!dbState.has(table.name)) { lines.push(buildCreateTable(table), ''); hasChanges = true; }
  }

  for (const table of schema.tables) {
    if (!dbState.has(table.name)) continue;
    const { columns: dbCols, indexes: dbIdxs } = dbState.get(table.name);
    const addParts     = [];
    const dropComments = [];

    for (const col of table.columns) {
      if (!dbCols.has(col.name)) {
        const mysqlType = getMysqlType(col.type);
        const notNull   = (!isColNullable(col)) ? ' NOT NULL' : ' NULL';
        const autoInc   = col.auto ? ' AUTO_INCREMENT' : '';
        const dflt      = sqlDefaultClause(col);
        addParts.push(`  ADD COLUMN \`${col.name}\` ${mysqlType}${notNull}${autoInc}${dflt}`);
        hasChanges = true;
      }
    }

    const schemaCols = new Set(table.columns.map(c => c.name));
    for (const dbCol of dbCols) {
      if (!schemaCols.has(dbCol)) {
        const stmt = `ALTER TABLE \`${table.name}\` DROP COLUMN \`${dbCol}\`;`;
        dropComments.push(ALLOW_DROPS ? stmt : `-- ${stmt}`);
        hasChanges = true;
      }
    }

    const schemaIdxs = new Map();
    for (const col of table.columns) {
      if (col.unique) schemaIdxs.set(`ux_${table.name}_${col.name}`, { columns: [col.name], unique: true });
    }
    for (const idx of (table.indexes || [])) schemaIdxs.set(idx.name, idx);

    for (const [idxName, idx] of schemaIdxs) {
      if (!dbIdxs.has(idxName)) {
        const unique = idx.unique ? 'UNIQUE ' : '';
        const cols   = idx.columns.map(c => `\`${c}\``).join(', ');
        addParts.push(`  ADD ${unique}INDEX \`${idxName}\` (${cols})`);
        hasChanges = true;
      }
    }

    for (const dbIdx of dbIdxs) {
      if (dbIdx === 'PRIMARY' || schemaIdxs.has(dbIdx)) continue;
      const stmt = `ALTER TABLE \`${table.name}\` DROP INDEX \`${dbIdx}\`;`;
      dropComments.push(ALLOW_DROPS ? stmt : `-- ${stmt}`);
      hasChanges = true;
    }

    if (addParts.length) { lines.push(`ALTER TABLE \`${table.name}\`\n${addParts.join(',\n')};`, ''); }
    if (dropComments.length) {
      lines.push(`-- ${table.name}: removed from schema (review before applying):`);
      lines.push(...dropComments, '');
    }
  }

  for (const dbTableName of dbState.keys()) {
    if (!schemaTables.has(dbTableName)) {
      const stmt = `DROP TABLE IF EXISTS \`${dbTableName}\`;`;
      lines.push('-- Table removed from schema (review before applying):');
      lines.push(ALLOW_DROPS ? stmt : `-- ${stmt}`, '');
      hasChanges = true;
    }
  }

  return hasChanges ? lines.join('\n') : null;
}

// ── C# generation ─────────────────────────────────────────────────────────────

function buildEfProperty(col, dbClass) {
  const prop  = toPascal(col.name);
  const bt    = baseType(col.type);
  const parts = [`.HasColumnName(${dbClass}.Schema.${prop})`];

  if (!POMELO_NATIVE.has(bt)) {
    if (col.type.match(/^string\(\d+\)$/)) {
      parts.push(`.HasColumnType("VARCHAR(${col.type.match(/\((\d+)\)/)[1]})")`);
    } else {
      switch (bt) {
        case 'string':   parts.push('.HasColumnType("TEXT")');           break;
        case 'longtext': parts.push('.HasColumnType("LONGTEXT")');       break;
        case 'datetime': parts.push('.HasColumnType("datetime(6)")');    break;
        case 'date':     parts.push('.HasColumnType("date")');           break;
        case 'json':     parts.push('.HasColumnType("json")');           break;
        case 'uuid':     parts.push('.HasColumnType("CHAR(36)")');       break;
        case 'bytes':    parts.push('.HasColumnType("BLOB")');           break;
        case 'int8':     parts.push('.HasColumnType("TINYINT")');        break;
        case 'int16':    parts.push('.HasColumnType("SMALLINT")');       break;
        case 'uint8':    parts.push('.HasColumnType("TINYINT UNSIGNED")'); break;
        case 'uint16':   parts.push('.HasColumnType("SMALLINT UNSIGNED")'); break;
        case 'uint32':   parts.push('.HasColumnType("INT UNSIGNED")');   break;
        case 'uint64':   parts.push('.HasColumnType("BIGINT UNSIGNED")'); break;
      }
    }
  }

  const efDef = efDefaultValue(col);
  if (efDef !== null)             parts.push(`.HasDefaultValue(${efDef})`);
  if (col.auto)                   parts.push('.ValueGeneratedOnAdd()');
  if (!isColNullable(col))        parts.push('.IsRequired()');

  const pad = '            ';
  return `        builder.Property(e => e.${prop})\n${pad}${parts.join(`\n${pad}`)};`;
}

function rowPropertyLine(col) {
  const nullable = isColNullable(col);
  const csType   = getCsharpType(col.type, nullable);
  const bt       = baseType(col.type);

  if (!nullable && csType === 'string') return `    public ${csType} ${toPascal(col.name)} { get; set; } = string.Empty;`;
  if (!nullable && csType === 'byte[]') return `    public ${csType} ${toPascal(col.name)} { get; set; } = Array.Empty<byte>();`;
  return `    public ${csType} ${toPascal(col.name)} { get; set; }`;
}

function generateTableFile(table, schema) {
  const ns        = schema.output.namespace;
  const ctx       = schema.output.context;
  const rowClass  = `${toPascal(table.name)}Row`;
  const dbClass   = `${toPascal(table.name)}Db`;
  const cfgClass  = `${toPascal(table.name)}DbConfiguration`;
  const dbSetProp = `_${toPascal(table.name)}`;
  const pkCols    = table.columns.filter(c => c.pk);

  // ── Row properties ─────────────────────────────────────────────────────────
  const rowPropLines = table.columns.map(rowPropertyLine);
  for (const fk of fkColumns(table)) {
    const refTable = tableByName(schema, fk.refTableName);
    rowPropLines.push(`    public ${toPascal(refTable.name)}Row? ${singularPascal(refTable.name)} { get; set; }`);
  }
  const rowProps = rowPropLines.join('\n');

  // ── EF configuration body ──────────────────────────────────────────────────
  const efBody = [];

  efBody.push(`        builder.ToTable(${dbClass}.Schema.Table);`);

  if (pkCols.length > 1) {
    efBody.push(`        builder.HasKey(e => new { ${pkCols.map(c => `e.${toPascal(c.name)}`).join(', ')} });`);
  } else {
    efBody.push(`        builder.HasKey(e => e.${toPascal(pkCols[0].name)});`);
  }
  efBody.push('');

  for (const col of table.columns) efBody.push(buildEfProperty(col, dbClass));

  const uniqueCols = table.columns.filter(c => c.unique);
  const tableIdxs  = table.indexes || [];

  if (uniqueCols.length || tableIdxs.length) efBody.push('');

  for (const col of uniqueCols) {
    efBody.push(`        builder.HasIndex(e => e.${toPascal(col.name)}).IsUnique().HasDatabaseName("ux_${table.name}_${col.name}");`);
  }

  for (const idx of tableIdxs) {
    const expr = idx.columns.length === 1
      ? `e.${toPascal(idx.columns[0])}`
      : `new { ${idx.columns.map(c => `e.${toPascal(c)}`).join(', ')} }`;
    const uq = idx.unique ? '.IsUnique()' : '';
    efBody.push(`        builder.HasIndex(e => ${expr})${uq}.HasDatabaseName("${idx.name}");`);
  }

  const ownFks = fkColumns(table);
  if (ownFks.length) efBody.push('');
  for (const fk of ownFks) {
    const refTable = tableByName(schema, fk.refTableName);
    efBody.push(
      `        builder.HasOne(e => e.${singularPascal(refTable.name)})\n` +
      `            .WithMany()\n` +
      `            .HasForeignKey(e => e.${toPascal(fk.column.name)})\n` +
      `            .OnDelete(DeleteBehavior.Restrict);`
    );
  }

  // ── Schema constants ───────────────────────────────────────────────────────
  const schemaConsts = [
    `        public const string Table = "${table.name}";`,
    ...table.columns.map(c => `        public const string ${toPascal(c.name)} = "${c.name}";`),
  ].join('\n');

  // ── FindAsync signature ────────────────────────────────────────────────────
  const findParams = pkCols.map(c => `${getCsharpBaseType(c.type)} ${toCamel(c.name)}`).join(', ');
  const findArgs   = pkCols.map(c => toCamel(c.name)).join(', ');

  const uniqueFindCols = new Map();
  for (const col of uniqueCols) uniqueFindCols.set(col.name, col);
  for (const idx of tableIdxs) {
    if (idx.unique && idx.columns.length === 1) {
      uniqueFindCols.set(idx.columns[0], columnByName(table, idx.columns[0]));
    }
  }

  const uniqueFindMethods = [...uniqueFindCols.values()].map(col => {
    const prop = toPascal(col.name);
    const param = toCamel(col.name);
    const paramType = getCsharpType(col.type, isColNullable(col));
    return `    public Task<${rowClass}?> FindBy${prop}Async(${paramType} ${param}, CancellationToken ct = default)\n` +
      `        => _db.${dbSetProp}.FirstOrDefaultAsync(e => e.${prop} == ${param}, ct);`;
  });

  const joinMethods = [];
  const joinMethodNames = new Set();

  for (const fk of ownFks) {
    const refTable = tableByName(schema, fk.refTableName);
    const refRow = `${toPascal(refTable.name)}Row`;
    const refDbSet = `_${toPascal(refTable.name)}`;
    let methodName = `JoinWith${toPascal(refTable.name)}`;
    if (joinMethodNames.has(methodName)) methodName = `${methodName}By${toPascal(fk.column.name)}`;
    joinMethodNames.add(methodName);
    joinMethods.push(
      `    public IQueryable<(${rowClass} ${singularPascal(table.name)}, ${refRow} ${singularPascal(refTable.name)})> ${methodName}()\n` +
      `        => Query().Join(_db.${refDbSet}, l => l.${toPascal(fk.column.name)}, r => r.${toPascal(fk.refColumnName)},\n` +
      `            (l, r) => ValueTuple.Create(l, r));`
    );
  }

  for (const fk of referencingFks(table, schema)) {
    const ownerRow = `${toPascal(fk.owner.name)}Row`;
    const ownerDbSet = `_${toPascal(fk.owner.name)}`;
    let methodName = `JoinWith${toPascal(fk.owner.name)}`;
    if (joinMethodNames.has(methodName)) methodName = `${methodName}By${toPascal(fk.column.name)}`;
    joinMethodNames.add(methodName);
    joinMethods.push(
      `    public IQueryable<(${rowClass} ${singularPascal(table.name)}, ${ownerRow} ${singularPascal(fk.owner.name)})> ${methodName}()\n` +
      `        => Query().Join(_db.${ownerDbSet}, l => l.${toPascal(fk.refColumnName)}, r => r.${toPascal(fk.column.name)},\n` +
      `            (l, r) => ValueTuple.Create(l, r));`
    );
  }

  const insertIgnoreMethod = table.conflict === 'ignore' ? (() => {
    const insertCols = table.columns.filter(c => !c.auto);
    const colList    = insertCols.map(c => `\`${c.name}\``).join(', ');
    const valList    = insertCols.map(getInsertExpr).join(', ');
    return `\n    public Task InsertIgnoreAsync(${rowClass} row, CancellationToken ct = default)\n` +
      `        => _db.Database.ExecuteSqlInterpolatedAsync(\n` +
      `            $"INSERT IGNORE INTO \`${table.name}\` (${colList}) VALUES (${valList})",\n` +
      `            ct);\n`;
  })() : '';

  return `// <auto-generated> Do not edit. Regenerate via: npm run gen:orm </auto-generated>
#nullable enable

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ${ns};

public sealed class ${rowClass}
{
${rowProps}
}

internal sealed class ${cfgClass} : IEntityTypeConfiguration<${rowClass}>
{
    public void Configure(EntityTypeBuilder<${rowClass}> builder)
    {
${efBody.join('\n')}
    }
}

public sealed class ${dbClass}
{
    public static class Schema
    {
${schemaConsts}
    }

    private readonly ${ctx} _db;
    internal ${dbClass}(${ctx} db) => _db = db;

    public ValueTask<${rowClass}?> FindAsync(${findParams}, CancellationToken ct = default)
        => _db.${dbSetProp}.FindAsync(new object[] { ${findArgs} }, ct);

${uniqueFindMethods.length ? `${uniqueFindMethods.join('\n\n')}\n` : ''}
    public void Insert(${rowClass} row) => _db.${dbSetProp}.Add(row);
    public void Delete(${rowClass} row) => _db.${dbSetProp}.Remove(row);
${insertIgnoreMethod}
    public IQueryable<${rowClass}> Query() => _db.${dbSetProp}.AsQueryable();
${joinMethods.length ? `\n${joinMethods.join('\n\n')}` : ''}
}
`;
}

function generateContextFile(schema) {
  const ns  = schema.output.namespace;
  const ctx = schema.output.context;
  const tables = schema.tables;

  const dbSetLines = tables.map(t =>
    `    internal DbSet<${toPascal(t.name)}Row> _${toPascal(t.name)} => Set<${toPascal(t.name)}Row>();`
  );
  const pubPropLines = tables.map(t =>
    `    public ${toPascal(t.name)}Db ${toPascal(t.name)} { get; }`
  );
  const ctorLines = tables.map(t =>
    `        ${toPascal(t.name)} = new ${toPascal(t.name)}Db(this);`
  );
  const cfgLines = tables.map(t =>
    `        mb.ApplyConfiguration(new ${toPascal(t.name)}DbConfiguration());`
  );

  return `// <auto-generated> Do not edit. Regenerate via: npm run gen:orm </auto-generated>
#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ${ns};

public sealed partial class ${ctx} : DbContext
{
${dbSetLines.join('\n')}

${pubPropLines.join('\n')}

    public ${ctx}(DbContextOptions<${ctx}> options) : base(options)
    {
${ctorLines.join('\n')}
    }

    public Task SaveAsync(CancellationToken ct = default) => SaveChangesAsync(ct);

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);
${cfgLines.join('\n')}
    }
}
`;
}

// ── DB interaction ─────────────────────────────────────────────────────────────

async function ensureDriver() {
  try { require.resolve('mysql2'); return; } catch {}
  console.log('[db-generator] mysql2 not found — installing...');
  const { execSync } = require('child_process');
  execSync('npm install mysql2 --no-save', { stdio: 'inherit', cwd: path.join(cfg.root, 'tools') });
}

async function getDbState(dbName) {
  const mysql2 = require('mysql2/promise');
  const conn = await mysql2.createConnection({
    host:     cfg.db.host,
    port:     cfg.db.port,
    database: dbName,
    user:     cfg.db.user,
    password: cfg.db.password,
  });

  try {
    const [colRows] = await conn.execute(
      `SELECT TABLE_NAME, COLUMN_NAME
       FROM INFORMATION_SCHEMA.COLUMNS
       WHERE TABLE_SCHEMA = ?
       ORDER BY TABLE_NAME, ORDINAL_POSITION`,
      [dbName]
    );
    const [idxRows] = await conn.execute(
      `SELECT TABLE_NAME, INDEX_NAME
       FROM INFORMATION_SCHEMA.STATISTICS
       WHERE TABLE_SCHEMA = ?
       GROUP BY TABLE_NAME, INDEX_NAME`,
      [dbName]
    );

    const state = new Map();
    for (const { TABLE_NAME, COLUMN_NAME } of colRows) {
      if (!state.has(TABLE_NAME)) state.set(TABLE_NAME, { columns: new Set(), indexes: new Set() });
      state.get(TABLE_NAME).columns.add(COLUMN_NAME);
    }
    for (const { TABLE_NAME, INDEX_NAME } of idxRows) {
      if (!state.has(TABLE_NAME)) state.set(TABLE_NAME, { columns: new Set(), indexes: new Set() });
      state.get(TABLE_NAME).indexes.add(INDEX_NAME);
    }
    return { conn, dbState: state };
  } catch (err) {
    await conn.end();
    throw err;
  }
}

async function executeSQL(conn, sql) {
  // Filter out commented lines, split into statements, execute each
  const executable = sql
    .split('\n')
    .filter(l => !l.trimStart().startsWith('--'))
    .join('\n');

  const stmts = executable
    .split(';')
    .map(s => s.trim())
    .filter(Boolean);

  for (const stmt of stmts) {
    await conn.execute(stmt);
  }
}

// ── File I/O ──────────────────────────────────────────────────────────────────

function rel(filePath) {
  return path.relative(cfg.root, filePath).replaceAll(path.sep, '/');
}

function createFileState() {
  return { expected: new Set(), changed: [], deleted: [], unchangedCount: 0 };
}

function writeGeneratedFile(filePath, content, state) {
  const fullPath = path.resolve(filePath);
  state.expected.add(fullPath);

  if (fs.existsSync(fullPath)) {
    const current = fs.readFileSync(fullPath, 'utf-8');
    if (current === content) {
      state.unchangedCount++;
      return false;
    }
  }

  state.changed.push(rel(fullPath));
  if (!CHECK_ONLY) {
    fs.mkdirSync(path.dirname(fullPath), { recursive: true });
    fs.writeFileSync(fullPath, content, 'utf-8');
  }
  return true;
}

function cleanupStaleGeneratedFiles(dir, state) {
  if (!fs.existsSync(dir)) return;
  for (const f of fs.readdirSync(dir)) {
    if (!f.endsWith('.g.cs')) continue;
    const fullPath = path.resolve(dir, f);
    if (state.expected.has(fullPath)) continue;

    state.deleted.push(rel(fullPath));
    if (!CHECK_ONLY) fs.unlinkSync(fullPath);
  }
}

function writeMigrationFile(sql) {
  const dir = cfg.paths.migrationsDir;
  if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });
  const ts   = new Date().toISOString().replace(/[:.]/g, '-').slice(0, 19);
  const file = path.join(dir, `${ts}_schema_sync.sql`);
  fs.writeFileSync(file, sql, 'utf-8');
  return file;
}

// ── Error helper ──────────────────────────────────────────────────────────────

function die(msg) {
  console.error(`[db-generator] ${msg}`);
  process.exit(1);
}

// ── Main ──────────────────────────────────────────────────────────────────────

async function main() {
  const schema = loadSchema();
  validateSchema(schema);

  const genDir = cfg.paths.ormGeneratedDir;
  const fileState = createFileState();

  // Generate C# files
  for (const table of schema.tables) {
    const content  = generateTableFile(table, schema);
    const filename = `${toPascal(table.name)}Db.g.cs`;
    if (writeGeneratedFile(path.join(genDir, filename), content, fileState)) {
      console.log(`  wrote ${filename}`);
    }
  }
  const ctxContent  = generateContextFile(schema);
  const ctxFilename = `${schema.output.context}.g.cs`;
  if (writeGeneratedFile(path.join(genDir, ctxFilename), ctxContent, fileState)) {
    console.log(`  wrote ${ctxFilename}`);
  }
  cleanupStaleGeneratedFiles(genDir, fileState);

  if (CHECK_ONLY) {
    if (fileState.changed.length > 0 || fileState.deleted.length > 0) {
      die(`generated ORM is out of date (changed=${fileState.changed.length} deleted=${fileState.deleted.length})`);
    }
    console.log(`[db-generator] ORM up to date (files=${fileState.unchangedCount}).`);
    return;
  }

  // Generate / apply migration SQL
  if (GENERATE_ONLY) {
    const sql  = buildMigrationSql(schema, null);
    const file = writeMigrationFile(sql);
    console.log(`[db-generator] Generated ${schema.tables.length} tables (generate-only)`);
    console.log(`[db-generator] Migration SQL → ${path.relative(cfg.root, file)}`);
    return;
  }

  await ensureDriver();

  let conn, dbState;
  try {
    ({ conn, dbState } = await getDbState(schema.database));
  } catch (err) {
    if (cfg.ormGen.dryRun) {
      console.warn(`[db-generator] DB connection failed: ${err.message}. Falling back to generate-only mode.`);
      const sql  = buildMigrationSql(schema, null);
      const file = writeMigrationFile(sql);
      console.log(`[db-generator] Generated ${schema.tables.length} tables (generate-only fallback)`);
      console.log(`[db-generator] Migration SQL → ${path.relative(cfg.root, file)}`);
      return;
    }
    die(`DB connection failed: ${err.message}`);
  }

  try {
    const sql = buildMigrationSql(schema, dbState);

    if (!sql) {
      console.log('[db-generator] No schema changes detected.');
      return;
    }

    const file = writeMigrationFile(sql);
    console.log(`[db-generator] Migration SQL → ${path.relative(cfg.root, file)}`);

    if (!cfg.ormGen.dryRun) {
      console.log('[db-generator] Executing migration...');
      await executeSQL(conn, sql);
      console.log('[db-generator] Migration applied.');
    } else {
      console.log('[db-generator] dry_run=true — review SQL and apply manually.');
    }
  } finally {
    await conn.end();
  }
}

if (require.main === module) {
  main().catch(err => die(err.message));
}

module.exports = {
  collectSchemaErrors,
  validateSchema,
};
