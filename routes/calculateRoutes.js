const express = require('express');
const { calculateTransfer } = require('../controllers/calculateController');

const router = express.Router();

try {
  router.post('/', calculateTransfer);
  console.log(' Connected');
} catch (error) {
  console.error(' Failed', { message: error.message });
}

module.exports = router;
