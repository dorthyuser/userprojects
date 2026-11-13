const express = require('express');
const { calculateLifetime } = require('../../controllers/postgresql/postgresqlController');

const router = express.Router();

try {
  router.post('/', calculateLifetime);
  console.log('PostgreSql Connected');
} catch (error) {
  console.log('PostgreSql Failed', error && error.message ? error.message : 'Route registration error');
}

module.exports = router;
