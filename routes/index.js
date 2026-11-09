const express = require('express');
const postgresqlRoutes = require('./postgresql/postgresqlRoutes');

const router = express.Router();

try {
  router.use('/lifetime-calculator', postgresqlRoutes);
  console.log('Routes Connected');
} catch (error) {
  console.error('Routes Failed', { message: error.message });
}

module.exports = router;
