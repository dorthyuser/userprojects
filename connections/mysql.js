const mysql = require('mysql2/promise');
const mysqlConnectionConfig = require('./config/mysql');

let mysqlConnectionInstance;

try {
  mysqlConnectionInstance = mysql.createPool(mysqlConnectionConfig);
  mysqlConnectionInstance.getConnection()
    .then((connection) => {
      console.log('mysql Connected');
      connection.release();
    })
    .catch((error) => {
      console.error('mysql Failed', {
        message: error.message,
        code: error.code || 'MYSQL_CONNECTION_ERROR'
      });
    });
} catch (error) {
  console.error('mysql Failed', {
    message: error.message,
    code: 'MYSQL_POOL_INIT_ERROR'
  });
}

module.exports = mysqlConnectionInstance;
