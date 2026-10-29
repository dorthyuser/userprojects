try {
  const mysqlConfig = {
    host: process.env.MYSQL_HOST,
    port: Number(process.env.MYSQL_PORT),
    database: process.env.MYSQL_DATABASE,
    user: process.env.MYSQL_USERNAME,
    password: process.env.MYSQL_PASSWORD,
    waitForConnections: true,
    connectionLimit: 10,
    queueLimit: 0,
    ssl: process.env.MYSQL_SSL === 'true' ? { rejectUnauthorized: true } : undefined
  };

  console.log('mysqlConfig Connected');
  module.exports = mysqlConfig;
} catch (error) {
  console.error('mysqlConfig Failed', {
    message: error.message,
    code: 'MYSQL_CONFIG_INIT_ERROR'
  });
  module.exports = {};
}
