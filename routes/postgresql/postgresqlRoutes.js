const express = require('express');
const router = express.Router();
const postgresqlController = require('../../controllers/postgresql/postgresqlController');

try {
  router.post('/auth/register', postgresqlController.registerUser);
  router.post('/auth/login', postgresqlController.loginUser);
  router.post('/auth/refresh', postgresqlController.refreshToken);
  router.get('/restaurants', postgresqlController.listRestaurants);
  router.get('/restaurants/:id/menu', postgresqlController.getRestaurantMenu);
  router.post('/orders', postgresqlController.createOrder);
  router.get('/orders/:id', postgresqlController.getOrderById);
  router.post('/delivery/:delivery_id/location', postgresqlController.updateDeliveryLocation);
  router.get('/delivery/:delivery_id/location', postgresqlController.getDeliveryLocations);
  console.log('postgresqlRoutes Connected');
} catch (error) {
  console.error('postgresqlRoutes Failed', error);
}

module.exports = router;
