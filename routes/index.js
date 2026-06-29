const express = require('express');
const healthRoutes = require('./healthRoutes');
const currenciesRoutes = require('./currenciesRoutes');
const ratesRoutes = require('./ratesRoutes');
const calculateRoutes = require('./calculateRoutes');

const router = express.Router();

try {
  router.use('/health', healthRoutes);
  router.use('/currencies', currenciesRoutes);
  router.use('/rates', ratesRoutes);
  router.use('/calculate', calculateRoutes);
  console.log(' Connected');
} catch (error) {
  console.error(' Failed', { message: error.message });
}

module.exports = router;
