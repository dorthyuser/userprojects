const express = require('express');
const { calculateLifetime } = require('../controllers/lifetimeCalculatorController');

const router = express.Router();

try {
  router.post('/', calculateLifetime);
  console.log(' Connected');
} catch (error) {
  console.error(' Failed', error);
}

module.exports = router;
