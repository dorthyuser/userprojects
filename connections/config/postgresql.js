module.exports = {
  host: process.env.POSTGRESQL_HOST,
  port: process.env.POSTGRESQL_PORT,
  database: process.env.POSTGRESQL_DATABASE,
  username: process.env.POSTGRESQL_USERNAME,
  password: process.env.POSTGRESQL_PASSWORD,
  sslmode: process.env.POSTGRESQL_SSLMODE,
  ssl: { rejectUnauthorized: false }
};
