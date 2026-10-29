try {
  const mysqlSystemConfig = {
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

  console.log('mysql connection config Connected');
  module.exports = mysqlSystemConfig;
} catch (error) {
  console.error('mysql connection config Failed', {
    message: error.message,
    code: 'MYSQL_CONNECTION_CONFIG_ERROR'
  });
  module.exports = {};
}
