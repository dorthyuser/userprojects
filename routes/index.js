const express = require('express');
const postgresqlRoutes = require('./postgresql/postgresqlRoutes');

const router = express.Router();

try {
  router.use('/lifetime-calculator', postgresqlRoutes);
  console.log('PostgreSql Connected');
} catch (error) {
  console.log('PostgreSql Failed', error && error.message ? error.message : 'Route setup error');
}

module.exports = router;
