const express = require('express');
const router = express.Router();
const postgresqlController = require('../../controllers/postgresql/postgresqlController');

try {
  router.post('/', postgresqlController.calculateLifetime);
  console.log('postgresql routes Connected');
} catch (error) {
  console.error('postgresql routes Failed', error && error.message ? error.message : 'Route setup error');
}

module.exports = router;
