const express = require('express');
const router = express.Router();

try {
  console.log(' Connected');
  router.use('/auth', require('./authRoutes'));
  router.use('/families', require('./familyRoutes'));
  router.use('/accounts', require('./accountRoutes'));
  router.use('/transactions', require('./transactionRoutes'));
  router.use('/budgets', require('./budgetRoutes'));
} catch (error) {
  console.log(' Failed', error && error.message ? error.message : error);
}

module.exports = router;
