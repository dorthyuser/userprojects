const express = require('express');
const router = express.Router();
const transactionController = require('../controllers/transactionController');

try {
  console.log(' Connected');
  router.post('/', transactionController.createTransaction);
  router.get('/', transactionController.listTransactions);
} catch (error) {
  console.log(' Failed', error && error.message ? error.message : error);
}

module.exports = router;
