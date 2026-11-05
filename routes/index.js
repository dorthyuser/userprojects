const express = require('express');
const router = express.Router();

try {
  router.use('/passwords', require('./mysql/mysqlRoutes'));
  console.log('mysql Connected');
} catch (error) {
  console.error('mysql Failed', error && error.message ? error.message : error);
}

module.exports = router;
