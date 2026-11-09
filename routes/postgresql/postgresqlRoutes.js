const express = require('express');
const postgresqlController = require('../../controllers/postgresql/postgresqlController');

const router = express.Router();

try {
  router.post('/', postgresqlController.calculateLifetime);
  console.log('PostgreSql Routes Connected');
} catch (error) {
  console.error('PostgreSql Routes Failed', { message: error.message });
}

module.exports = router;
