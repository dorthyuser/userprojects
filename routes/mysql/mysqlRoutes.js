const express = require('express');
const router = express.Router();
const mysqlController = require('../../controllers/mysql/mysqlController');

try {
  router.get('/health', mysqlController.health);
  router.post('/calculate', mysqlController.calculateAge);
  console.log('mysqlRoutes Connected');
} catch (error) {
  console.error('mysqlRoutes Failed', {
    message: error.message,
    code: 'MYSQL_ROUTES_INIT_ERROR'
  });
}

module.exports = router;
