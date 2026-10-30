const express = require('express');
const router = express.Router();

try {
  const lifetimeRoutes = require('./lifetimeCalculator');
  router.use('/', lifetimeRoutes);
} catch (error) {
}

module.exports = router;
