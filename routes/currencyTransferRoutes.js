const express = require('express');
const router = express.Router();
const controller = require('../controllers/currencyTransferController');

try {
  router.get('/health', controller.health);
  router.get('/currencies', controller.getCurrencies);
  router.get('/rates', controller.getRates);
  router.get('/swagger', controller.getSwagger);
  router.post('/validate', controller.validateTransfer);
  router.post('/calculate', controller.calculateTransfer);
  console.log(' Connected');
} catch (error) {
  console.error(' Failed', error && error.message ? error.message : error);
}

module.exports = router;