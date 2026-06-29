const express = require('express');
const { getHealth } = require('../controllers/healthController');

const router = express.Router();

try {
  router.get('/', getHealth);
  console.log(' Connected');
} catch (error) {
  console.error(' Failed', { message: error.message });
}

module.exports = router;
