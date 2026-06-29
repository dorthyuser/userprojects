const express = require('express');
const { getRates } = require('../controllers/rateController');

const router = express.Router();

try {
  router.get('/', getRates);
  console.log(' Connected');
} catch (error) {
  console.error(' Failed', { message: error.message });
}

module.exports = router;
