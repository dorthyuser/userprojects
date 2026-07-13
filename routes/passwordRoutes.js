const express = require('express');
const { generatePassword } = require('../controllers/passwordController');

const router = express.Router();

try {
  router.post('/generate', generatePassword);
  console.log(' Connected');
} catch (error) {
  console.error(' Failed', { message: error.message, code: error.code || 'ROUTE_BIND_ERROR' });
}

module.exports = router;
