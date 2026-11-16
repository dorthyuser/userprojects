const express = require('express');
const lifetimeCalculatorRoutes = require('./lifetimeCalculatorRoutes');

const router = express.Router();

try {
  router.use('/lifetime-calculator', lifetimeCalculatorRoutes);
  console.log(' Connected');
} catch (error) {
  console.error(' Failed', error && error.message ? error.message : error);
}

module.exports = router;
