const { Pool } = require('pg');
const dotenv = require('dotenv');

dotenv.config();

let postgresqlPool;

try {
  postgresqlPool = new Pool({
    host: process.env.POSTGRESQL_HOST,
    port: process.env.POSTGRESQL_PORT ? Number(process.env.POSTGRESQL_PORT) : 5432,
    database: process.env.POSTGRESQL_DATABASE,
    user: process.env.POSTGRESQL_USERNAME,
    password: process.env.POSTGRESQL_PASSWORD,
    ssl: { rejectUnauthorized: false }
  });

  postgresqlPool.connect()
    .then((client) => {
      client.release();
      console.log('postgresql Connected');
    })
    .catch((error) => {
      console.error('postgresql Failed', error && error.message ? error.message : 'Connection error');
    });
} catch (error) {
  console.error('postgresql Failed', error && error.message ? error.message : 'Initialization error');
}

module.exports = postgresqlPool;
