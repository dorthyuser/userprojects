const dotenv = require('dotenv');

dotenv.config();

const postgresqlConfig = {
  host: process.env.POSTGRESQL_HOST,
  port: process.env.POSTGRESQL_PORT,
  database: process.env.POSTGRESQL_DATABASE,
  dbname: process.env.POSTGRESQL_DBNAME,
  user: process.env.POSTGRESQL_USER,
  username: process.env.POSTGRESQL_USERNAME,
  password: process.env.POSTGRESQL_PASSWORD,
  ssl: { rejectUnauthorized: false }
};

try {
  console.log('postgresql Connected');
} catch (error) {
  console.error('postgresql Failed', error);
}

module.exports = postgresqlConfig;
