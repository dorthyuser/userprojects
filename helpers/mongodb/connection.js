const { Pool } = require("pg");

let pool;

function getPgConfig() {
  const host = process.env.DB_HOST;
  const port = process.env.DB_PORT;
  const user = process.env.DB_USER;
  const password = process.env.DB_PASSWORD;
  const database = process.env.DB_NAME;
  const sslMode = process.env.DB_SSL_MODE || "require";

  if (!host || !port || !user || !password || !database) {
    throw new Error("Database configuration is incomplete");
  }

  return {
    host,
    port: Number(port),
    user,
    password,
    database,
    ssl: sslMode === "require" ? { rejectUnauthorized: false } : false,
  };
}

function getPool() {
  if (!pool) {
    pool = new Pool(getPgConfig());
  }
  return pool;
}

async function getDatabase() {
  return {
    collection(name) {
      throw new Error(`Collection access is not supported for ${name}`);
    },
    query: async () => {
      throw new Error("Direct query helper not implemented");
    },
    pool: getPool(),
  };
}

module.exports = { getDatabase, getPool };
