const { Pool } = require('pg');

function createDb({ logger }) {
  const connectionString = process.env.POSTGRESQL_CONNECTION_STRING || buildConnectionString();
  const sslRejectUnauthorized = String(process.env.POSTGRESQL_SSL_REJECT_UNAUTHORIZED || 'false').toLowerCase() === 'true';

  const pool = new Pool({
    connectionString,
    host: process.env.POSTGRESQL_HOST,
    port: process.env.POSTGRESQL_PORT ? Number(process.env.POSTGRESQL_PORT) : undefined,
    database: process.env.POSTGRESQL_DATABASE,
    user: process.env.POSTGRESQL_USERNAME,
    password: process.env.POSTGRESQL_PASSWORD,
    application_name: process.env.POSTGRESQL_APPLICATION_NAME || 'combined_project',
    connectionTimeoutMillis: process.env.POSTGRESQL_CONNECT_TIMEOUT ? Number(process.env.POSTGRESQL_CONNECT_TIMEOUT) * 1000 : 30000,
    ssl: process.env.POSTGRESQL_SSLROOTCERT || sslRejectUnauthorized ? { rejectUnauthorized: sslRejectUnauthorized } : undefined
  });

  pool.on('error', (err) => {
    logger.error({ err: { message: err.message, code: err.code } }, 'postgres_pool_error');
  });

  return {
    async query(text, params) {
      return pool.query(text, params);
    },
    async transaction(work) {
      const client = await pool.connect();
      try {
        await client.query('BEGIN');
        const result = await work(client);
        await client.query('COMMIT');
        return result;
      } catch (error) {
        try {
          await client.query('ROLLBACK');
        } catch (_) {
        }
        throw error;
      } finally {
        client.release();
      }
    },
    async close() {
      await pool.end();
    }
  };
}

function buildConnectionString() {
  const host = process.env.POSTGRESQL_HOST;
  const port = process.env.POSTGRESQL_PORT;
  const database = process.env.POSTGRESQL_DATABASE;
  const username = process.env.POSTGRESQL_USERNAME;
  const password = process.env.POSTGRESQL_PASSWORD;

  if (!host || !port || !database || !username || !password) {
    return undefined;
  }

  return `Host=${host};Port=${port};Database=${database};Username=${username};Password=${password};Pooling=true;Maximum Pool Size=100;`;
}

module.exports = { createDb };
