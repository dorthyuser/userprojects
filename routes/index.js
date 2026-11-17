const express = require('express');
const router = express.Router();

try {
  const mysqlRoutes = require('./mysql/mysqlRoutes');
  router.use('/', mysqlRoutes);
  console.log('mysql routes Connected');
} catch (error) {
  console.error('mysql routes Failed', error && error.message ? error.message : error);
}

module.exports = router;
