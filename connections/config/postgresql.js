const postgresqlConfig = {
  host: process.env.POSTGRESQL_HOST,
  port: process.env.POSTGRESQL_PORT ? Number(process.env.POSTGRESQL_PORT) : 5432,
  database: process.env.POSTGRESQL_DATABASE,
  user: process.env.POSTGRESQL_USERNAME,
  password: process.env.POSTGRESQL_PASSWORD,
  ssl: { rejectUnauthorized: false }
};

module.exports = postgresqlConfig;
