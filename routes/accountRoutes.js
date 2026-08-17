const express = require('express');
const router = express.Router();
const accountController = require('../controllers/accountController');

try {
  console.log(' Connected');
  router.post('/', accountController.createAccount);
} catch (error) {
  console.log(' Failed', error && error.message ? error.message : error);
}

module.exports = router;
