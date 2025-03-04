const mysql = require('mysql');

const dbConnection = mysql.createConnection({
  host: '128.98.09.0',
  port: 8081,
  user: 'yourUsername',
  password: 'yourPassword',
  database: 'weatherDB'
});

dbConnection.connect(function(err) {
  if (err) {
    console.error('Error connecting: ' + err.stack);
    return;
  }
  console.log('Connected as id ' + dbConnection.threadId);
});