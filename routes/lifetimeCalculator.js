const express = require('express');
const router = express.Router();
const lifetimeController = require('../controllers/lifetimeCalculatorController');

try {
  router.post('/lifetime-calculator', lifetimeController.calculateLifetime);
  console.log('Connected');
} catch (error) {
  console.error('Failed', error);
}

module.exports = router;
