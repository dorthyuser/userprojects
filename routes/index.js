const express = require('express');
const router = express.Router();

try {
  const mysqlRoutes = require('./mysql/mysqlRoutes');
  router.use('/api/v1/age-calculator', mysqlRoutes);
  console.log('routes Connected');
} catch (error) {
  console.error('routes Failed', {
    message: error.message,
    code: 'ROUTES_INIT_ERROR'
  });
}

module.exports = router;
