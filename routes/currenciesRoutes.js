const express = require('express');
const { getSupportedCurrencies } = require('../controllers/currencyController');

const router = express.Router();

try {
  router.get('/', getSupportedCurrencies);
  console.log(' Connected');
} catch (error) {
  console.error(' Failed', { message: error.message });
}

module.exports = router;
