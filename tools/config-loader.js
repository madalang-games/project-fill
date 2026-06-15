'use strict';
const fs = require('fs');
const path = require('path');

const ROOT = path.resolve(__dirname, '..');

class ConfigError extends Error {
  constructor(source, message) {
    super(`[config] ERROR: ${source}: ${message}`);
    this.name = 'ConfigError';
  }
}

function parseIni(content, sourcePath) {
  const cfg = {};
  let section = '_';
  for (const [index, raw] of content.split(/\r?\n/).entries()) {
    const line = raw.trim();
    if (!line || line.startsWith(';') || line.startsWith('#')) continue;
    const sec = line.match(/^\[(.+)\]$/);
    if (sec) {
      section = sec[1];
      cfg[section] = cfg[section] || {};
      continue;
    }
    const eq = line.indexOf('=');
    if (eq <= 0) {
      throw new ConfigError(`${sourcePath}:${index + 1}`, `invalid ini line "${raw}"`);
    }
    const k = line.slice(0, eq).trim();
    const v = line.slice(eq + 1).trim();
    cfg[section] = cfg[section] || {};
    cfg[section][k] = v;
  }
  return cfg;
}

function parseEnv(content, sourcePath) {
  const env = {};
  for (const [index, raw] of content.split(/\r?\n/).entries()) {
    const line = raw.trim();
    if (!line || line.startsWith('#')) continue;
    const eq = line.indexOf('=');
    if (eq <= 0) {
      throw new ConfigError(`${sourcePath}:${index + 1}`, `invalid env line "${raw}"`);
    }
    const k = line.slice(0, eq).trim();
    const v = line.slice(eq + 1).trim().replace(/^["']|["']$/g, '');
    env[k] = v;
  }
  return env;
}

function resolveEnvPath() {
  const explicit = process.env.CONFIG_ENV_FILE || process.env.ENV_FILE;
  if (explicit) {
    return path.isAbsolute(explicit) ? explicit : path.join(ROOT, explicit);
  }

  const rawEnv = process.env.CONFIG_ENV || process.env.GAME_ENV || process.env.NODE_ENV || process.env.ASPNETCORE_ENVIRONMENT || 'dev';
  const normalized = rawEnv.toLowerCase().startsWith('prod') ? 'prod' : 'dev';
  return path.join(ROOT, `.env.${normalized}`);
}

function rel(filePath) {
  return path.relative(ROOT, filePath).replaceAll(path.sep, '/');
}

function requiredIni(ini, sourcePath, section, key) {
  const value = ini[section]?.[key];
  if (value !== undefined && value !== '') return value;
  throw new ConfigError(`${rel(sourcePath)} [${section}].${key}`, 'missing required value');
}

function requiredSection(ini, sourcePath, section) {
  if (ini[section] && Object.keys(ini[section]).length > 0) {
    return ini[section];
  }

  throw new ConfigError(`${rel(sourcePath)} [${section}]`, 'missing required section');
}

function requiredEnv(env, sourcePath, key) {
  const value = env[key];
  if (value !== undefined && value !== '') return value;
  throw new ConfigError(`${rel(sourcePath)} ${key}`, 'missing required value');
}

function requiredIntEnv(env, sourcePath, key) {
  const raw = requiredEnv(env, sourcePath, key);
  const value = Number.parseInt(raw, 10);
  if (Number.isInteger(value)) return value;
  throw new ConfigError(`${rel(sourcePath)} ${key}`, `"${raw}" is not a valid integer`);
}

function requiredBoolIni(ini, sourcePath, section, key) {
  const raw = requiredIni(ini, sourcePath, section, key);
  if (raw === 'true') return true;
  if (raw === 'false') return false;
  throw new ConfigError(`${rel(sourcePath)} [${section}].${key}`, `"${raw}" is not a valid boolean`);
}

function requiredPath(ini, iniPath, section, key) {
  return path.join(ROOT, requiredIni(ini, iniPath, section, key));
}

function csv(value) {
  return value.split(',').map(s => s.trim()).filter(Boolean);
}

function load() {
  const iniPath = path.join(ROOT, 'template.ini');
  const envPath = resolveEnvPath();

  if (!fs.existsSync(iniPath)) {
    throw new ConfigError(rel(iniPath), 'file not found');
  }
  if (!fs.existsSync(envPath)) {
    throw new ConfigError(rel(envPath), 'file not found');
  }

  const ini = parseIni(fs.readFileSync(iniPath, 'utf-8'), rel(iniPath));
  const env = parseEnv(fs.readFileSync(envPath, 'utf-8'), rel(envPath));
  Object.assign(env, process.env);

  return {
    root: ROOT,
    envPath,

    paths: {
      datasDir: requiredPath(ini, iniPath, 'paths', 'datas_dir'),
      packetsDir: requiredPath(ini, iniPath, 'paths', 'packets_dir'),
      dbSchema: requiredPath(ini, iniPath, 'paths', 'db_schema'),
      clientGenerated: requiredPath(ini, iniPath, 'paths', 'client_generated'),
      clientScriptsGenerated: requiredPath(ini, iniPath, 'paths', 'client_scripts_generated'),
      serverGenerated: requiredPath(ini, iniPath, 'paths', 'server_generated'),
      serverScriptsGenerated: requiredPath(ini, iniPath, 'paths', 'server_scripts_generated'),
      migrationsDir: requiredPath(ini, iniPath, 'paths', 'migrations_dir'),
      ormGeneratedDir: requiredPath(ini, iniPath, 'paths', 'orm_generated_dir'),
      protoGenerated: requiredPath(ini, iniPath, 'paths', 'proto_generated'),
    },

    dataGen: {
      clientTargets: csv(requiredIni(ini, iniPath, 'data-gen', 'client_targets')),
      serverTargets: csv(requiredIni(ini, iniPath, 'data-gen', 'server_targets')),
      clientNamespace: requiredIni(ini, iniPath, 'data-gen', 'client_namespace'),
      serverNamespace: requiredIni(ini, iniPath, 'data-gen', 'server_namespace'),
      domainStaticDataDir: requiredPath(ini, iniPath, 'data-gen', 'domain_static_data_dir'),
      domainStaticDataNamespace: requiredIni(ini, iniPath, 'data-gen', 'domain_static_data_namespace'),
      domainInterfaceFile: requiredPath(ini, iniPath, 'data-gen', 'domain_interface_file'),
      domainInterfaceNamespace: requiredIni(ini, iniPath, 'data-gen', 'domain_interface_namespace'),
      infrastructureStaticDataServiceFile: requiredPath(ini, iniPath, 'data-gen', 'infrastructure_static_data_service_file'),
      infrastructureDataNamespace: requiredIni(ini, iniPath, 'data-gen', 'infrastructure_data_namespace'),
      gameTypesNamespace: requiredIni(ini, iniPath, 'data-gen', 'game_types_namespace'),
    },

    packetGen: {
      mode: requiredIni(ini, iniPath, 'packet-gen', 'mode'),
      clientLanguage: requiredIni(ini, iniPath, 'packet-gen', 'client_language'),
      serverLanguage: requiredIni(ini, iniPath, 'packet-gen', 'server_language'),
      clientNamespace: requiredIni(ini, iniPath, 'packet-gen', 'client_namespace'),
      serverNamespace: requiredIni(ini, iniPath, 'packet-gen', 'server_namespace'),
      clientOutputDir: requiredPath(ini, iniPath, 'packet-gen', 'client_output_dir'),
    },

    ormGen: {
      dryRun: requiredBoolIni(ini, iniPath, 'orm-gen', 'dry_run'),
    },

    fontSubset: {
      config: requiredPath(ini, iniPath, 'font-subset', 'config'),
      sourceDir: requiredPath(ini, iniPath, 'font-subset', 'source_dir'),
      targetDir: requiredPath(ini, iniPath, 'font-subset', 'target_dir'),
      stringDatasDir: requiredPath(ini, iniPath, 'font-subset', 'string_datas_dir'),
    },

    db: {
      type: 'mysql',
      host: requiredEnv(env, envPath, 'DB_BIND_ADDRESS'),
      port: requiredIntEnv(env, envPath, 'DB_PUBLISHED_PORT'),
      name: requiredEnv(env, envPath, 'DB_NAME'),
      user: requiredEnv(env, envPath, 'DB_USER'),
      password: requiredEnv(env, envPath, 'DB_PASSWORD'),
    },

    typeMap: {
      proto: requiredSection(ini, iniPath, 'type-map.proto'),
      csharp: requiredSection(ini, iniPath, 'type-map.csharp'),
      cpp: requiredSection(ini, iniPath, 'type-map.cpp'),
    },

    github: {
      token: env.GITHUB_TOKEN || '',
      repoUrl: env.GITHUB_REPO_URL || '',
      defaultProject: env.GITHUB_DEFAULT_PROJECT || '',
      defaultAssignee: env.GITHUB_DEFAULT_ASSIGNEE || '',
    },
  };
}

try {
  module.exports = load();
} catch (error) {
  console.error(error.message);
  process.exit(1);
}
