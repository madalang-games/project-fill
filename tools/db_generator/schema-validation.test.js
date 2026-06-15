'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const { collectSchemaErrors } = require('./index');

function baseSchema(tables) {
  return {
    database: 'test_db',
    output: {
      namespace: 'ProjectLink.Infrastructure.Generated',
      context: 'AppDbContext',
    },
    tables,
  };
}

test('accepts a valid schema with unique lookup and fk relationship', () => {
  const errors = collectSchemaErrors(baseSchema([
    {
      name: 'players',
      columns: [
        { name: 'user_id', type: 'int64', pk: true, null: false },
        { name: 'platform_pid', type: 'string(24)', null: false, unique: true },
      ],
    },
    {
      name: 'sessions',
      columns: [
        { name: 'id', type: 'int64', pk: true, auto: true, null: false },
        { name: 'user_id', type: 'int64', null: false, fk: 'players.user_id' },
      ],
      indexes: [
        { name: 'idx_sessions_user_id', columns: ['user_id'] },
      ],
    },
  ]));

  assert.deepEqual(errors, []);
});

test('rejects circular fk references before generation', () => {
  const errors = collectSchemaErrors(baseSchema([
    {
      name: 'first_table',
      columns: [
        { name: 'id', type: 'int64', pk: true, null: false },
        { name: 'second_id', type: 'int64', null: false, fk: 'second_table.id' },
      ],
    },
    {
      name: 'second_table',
      columns: [
        { name: 'id', type: 'int64', pk: true, null: false },
        { name: 'first_id', type: 'int64', null: false, fk: 'first_table.id' },
      ],
    },
  ]));

  assert.ok(errors.some(error => error.includes('Circular FK reference detected')));
});

test('accepts conflict:ignore on a table', () => {
  const errors = collectSchemaErrors(baseSchema([
    {
      name: 'sessions',
      conflict: 'ignore',
      columns: [
        { name: 'id', type: 'int64', pk: true, auto: true, null: false },
        { name: 'session_id', type: 'string(24)', null: false, unique: true },
      ],
    },
  ]));

  assert.deepEqual(errors, []);
});

test('rejects unknown conflict value', () => {
  const errors = collectSchemaErrors(baseSchema([
    {
      name: 'sessions',
      conflict: 'update',
      columns: [
        { name: 'id', type: 'int64', pk: true, auto: true, null: false },
      ],
    },
  ]));

  assert.ok(errors.some(e => e.includes('"conflict" must be "ignore"')));
});

test('reports multiple schema errors together', () => {
  const errors = collectSchemaErrors(baseSchema([
    {
      name: 'bad_table',
      columns: [
        { name: 'id', type: 'int64', auto: true, null: false },
        { name: 'id', type: 'int64', null: false },
      ],
      indexes: [
        { name: 'bad-index', columns: ['missing_column'] },
      ],
    },
  ]));

  assert.ok(errors.length >= 3);
  assert.ok(errors.some(error => error.includes('auto requires pk:true')));
  assert.ok(errors.some(error => error.includes('no PK column defined')));
  assert.ok(errors.some(error => error.includes('unknown column "missing_column"')));
});
