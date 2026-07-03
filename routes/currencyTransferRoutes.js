const express = require('express');
const router = express.Router();
const controller = require('../controllers/currencyTransferController');

try {
  router.get('/health', controller.healthCheck);
  router.get('/currencies', controller.getCurrencies);
  router.get('/rates', controller.getRates);
  router.post('/calculate', controller.calculateTransfer);
  console.log(' Connected');
} catch (error) {
  console.error(' Failed', { message: error.message });
}

module.exports = router;
