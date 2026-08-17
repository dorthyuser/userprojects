const express = require('express');
const router = express.Router();
const authController = require('../controllers/authController');

try {
  console.log(' Connected');
  router.post('/register', authController.register);
  router.post('/login', authController.login);
} catch (error) {
  console.log(' Failed', error && error.message ? error.message : error);
}

module.exports = router;
