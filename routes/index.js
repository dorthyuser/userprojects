const express = require('express');
const passwordRoutes = require('./passwordRoutes');

const router = express.Router();

try {
  router.use('/passwords', passwordRoutes);
  console.log(' Connected');
} catch (error) {
  console.error(' Failed', { message: error.message, code: error.code || 'ROUTE_INIT_ERROR' });
}

module.exports = router;
