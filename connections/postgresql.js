const { Pool } = require('pg');
const dotenv = require('dotenv');

dotenv.config();

let postgresqlPool;

try {
  postgresqlPool = new Pool({
    host: process.env.POSTGRESQL_HOST,
    port: Number(process.env.POSTGRESQL_PORT),
    database: process.env.POSTGRESQL_DATABASE,
    user: process.env.POSTGRESQL_USERNAME,
    password: process.env.POSTGRESQL_PASSWORD,
    ssl: { rejectUnauthorized: false }
  });

  postgresqlPool.connect()
    .then(client => {
      client.release();
      console.log('PostgreSql Connected');
    })
    .catch(error => {
      console.error('PostgreSql Failed', { message: error.message });
    });
} catch (error) {
  console.error('PostgreSql Failed', { message: error.message });
}

module.exports = postgresqlPool;
