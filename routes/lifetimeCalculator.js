const express = require('express');
const router = express.Router();
const lifetimeController = require('../controllers/lifetimeCalculatorController');

try {
  router.post('/lifetime-calculator', lifetimeController.calculateLifetime);
} catch (error) {
}

module.exports = router;
