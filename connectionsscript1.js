const yaml = require('js-yaml');
const fs = require('fs');

let config;
try {
  config = yaml.load(fs.readFileSync('config.yaml', 'utf8'));
} catch (e) {
  console.error(e);
}

const mysql = require('mysql');

const connection = mysql.createConnection({
  host: config.http.host,
  port: config.http.port,
  user: config.mysql.user,
  password: config.mysql.password,
  database: config.mysql.database
});

connection.connect((err) => {
  if (err) {
    console.error('Database connection failed: ' + err.stack);
    return;
  }
  console.log('Connected to database as id ' + connection.threadId);
});