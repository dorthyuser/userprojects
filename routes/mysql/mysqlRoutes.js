const express = require('express');
const router = express.Router();
const mysqlController = require('../../controllers/mysql/mysqlController');

try {
  router.get('/hotels', mysqlController.searchHotels);
  router.post('/bookings', mysqlController.createBooking);
  router.get('/bookings/:bookingId', mysqlController.getBookingById);
  router.post('/bookings/:bookingId/cancel', mysqlController.cancelBooking);
  console.log('mysqlRoutes Connected');
} catch (error) {
  console.error('mysqlRoutes Failed', error && error.message ? error.message : error);
}

module.exports = router;
