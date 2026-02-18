const { Pool } = require("pg");

let pool;

function getPool() {
  if (pool) return pool;
  pool = new Pool({
    host: process.env.PGHOST,
    user: process.env.PGUSER,
    password: process.env.PGPASSWORD,
    database: process.env.PGDATABASE,
    port: process.env.PGPORT ? Number(process.env.PGPORT) : 5432,
  });
  return pool;
}

module.exports = {
  getPool,
};
