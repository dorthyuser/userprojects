const express = require('express');
const router = express.Router();

try {
  const postgresqlRoutes = require('./postgresql/postgresqlRoutes');
  router.use('/lifetime-calculator', postgresqlRoutes);
  console.log('routes Connected');
} catch (error) {
  console.error('routes Failed', error && error.message ? error.message : 'Route loading error');
}

module.exports = router;
