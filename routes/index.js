const express = require('express');
const postgresqlRoutes = require('./postgresql/postgresqlRoutes');

const router = express.Router();

try {
  router.use('/orders', postgresqlRoutes);
  console.log(' Connected');
} catch (error) {
  console.error(' Failed', error.message);
}

module.exports = router;
