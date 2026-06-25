const express = require('express');
const router = express.Router();

try {
  const lifetimeRoutes = require('./lifetimeCalculator');
  router.use('/', lifetimeRoutes);
  console.log('Connected');
} catch (error) {
  console.error('Failed', error);
}

module.exports = router;
