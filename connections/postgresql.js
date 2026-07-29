const { Pool } = require('pg');
const dotenv = require('dotenv');

dotenv.config();

let postgresqlPoolInstance;

try {
  postgresqlPoolInstance = new Pool({
    host: process.env.POSTGRESQL_HOST,
    port: Number(process.env.POSTGRESQL_PORT),
    database: process.env.POSTGRESQL_DATABASE || process.env.POSTGRESQL_DBNAME,
    user: process.env.POSTGRESQL_USER || process.env.POSTGRESQL_USERNAME,
    password: process.env.POSTGRESQL_PASSWORD,
    ssl: { rejectUnauthorized: false }
  });

  postgresqlPoolInstance.on('error', (error) => {
    console.error('postgresql Failed', error);
  });

  postgresqlPoolInstance.connect()
    .then((client) => {
      console.log('postgresql Connected');
      client.release();
    })
    .catch((error) => {
      console.error('postgresql Failed', error);
    });
} catch (error) {
  console.error('postgresql Failed', error);
}

module.exports = postgresqlPoolInstance;
