const { Pool } = require('pg');
const postgresqlConfig = require('./config/postgresql');

let postgresqlConnection = null;

try {
  postgresqlConnection = new Pool(postgresqlConfig);
  postgresqlConnection.connect()
    .then(client => {
      client.release();
      console.log('PostgreSql Connected');
    })
    .catch(error => {
      console.log('PostgreSql Failed', error && error.message ? error.message : 'Connection error');
    });
} catch (error) {
  console.log('PostgreSql Failed', error && error.message ? error.message : 'Initialization error');
}

module.exports = { postgresqlConnection };
