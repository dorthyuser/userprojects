const express = require('express');
const router = express.Router();
const currencyTransferRoutes = require('./currencyTransferRoutes');

try {
  router.use('/', currencyTransferRoutes);
  console.log(' Connected');
} catch (error) {
  console.error(' Failed', { message: error.message });
}

module.exports = router;
