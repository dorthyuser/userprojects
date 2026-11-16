const express = require('express');
const { createAgeMetrics } = require('../controllers/ageMetricsController');

const router = express.Router();

try {
  router.post('/age-metrics', createAgeMetrics);
  console.log('Connected');
} catch (error) {
  console.error('Failed', error.message);
}

module.exports = router;
