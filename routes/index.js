const express = require('express');
const router = express.Router();

try {
  const postgresqlRoutes = require('./postgresql/postgresqlRoutes');
  router.use('/', postgresqlRoutes);
  console.log('routes Connected');
} catch (error) {
  console.error('routes Failed', error);
}

module.exports = router;
