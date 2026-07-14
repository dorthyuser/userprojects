const { Pool } = require('pg');
const dbOptions = require('./config/postgresql');

let postgresqlPoolInstance = null;

function initializePostgreSqlConnection() {
  try {
    if (!postgresqlPoolInstance) {
      postgresqlPoolInstance = new Pool({
        host: dbOptions.host,
        port: Number(dbOptions.port),
        database: dbOptions.database,
        user: dbOptions.username,
        ssl: dbOptions.ssl,
        application_name: dbOptions.application_name,
        connectionTimeoutMillis: Number(dbOptions.connect_timeout) * 1000,
        sslrootcert: dbOptions.sslrootcert
      });
      console.log(' Connected');
    }
    return postgresqlPoolInstance;
  } catch (error) {
    console.error(' Failed', error.message);
    throw error;
  }
}

module.exports = { initializePostgreSqlConnection, postgresqlPoolInstance };
