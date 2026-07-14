const dbOptions = {
  host: process.env.POSTGRESQL_HOST,
  port: process.env.POSTGRESQL_PORT,
  database: process.env.POSTGRESQL_DATABASE,
  username: process.env.POSTGRESQL_USERNAME,
  sslrootcert: process.env.POSTGRESQL_SSLROOTCERT,
  application_name: process.env.POSTGRESQL_APPLICATION_NAME,
  connect_timeout: process.env.POSTGRESQL_CONNECT_TIMEOUT,
  ssl: { rejectUnauthorized: false }
};

module.exports = dbOptions;
