const mysql = require('mysql');
const mysqlConfig = require('./config/mysql');

let mysqlConnection;
try {
  mysqlConnection = mysql.createConnection(mysqlConfig);
  mysqlConnection.connect((error) => {
    if (error) {
      console.error('mysql Failed', error && error.message ? error.message : error);
    } else {
      console.log('mysql Connected');
    }
  });
} catch (error) {
  console.error('mysql Failed', error && error.message ? error.message : error);
}

module.exports = mysqlConnection;
