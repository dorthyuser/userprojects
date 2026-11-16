const express = require('express');
const ageMetricsRoutes = require('./ageMetricsRoutes');

const router = express.Router();

try {
  router.use('/users', ageMetricsRoutes);
  console.log('Connected');
} catch (error) {
  console.error('Failed', error.message);
}

module.exports = router;
