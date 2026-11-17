require('dotenv').config();
const { HotelHandler } = require('./handlers/HotelHandler');
const { BookingHandler } = require('./handlers/BookingHandler');

exports.handler = async (event, context) => {
  const path = event && event.path ? event.path : '';
  const method = event && event.httpMethod ? event.httpMethod.toUpperCase() : '';

  if (path === '/hotels' && method === 'GET') return HotelHandler.searchHotels(event, context);
  if (path === '/bookings' && method === 'POST') return BookingHandler.createBooking(event, context);
  if (path && path.startsWith('/bookings/') && path.endsWith('/cancel') && method === 'POST') return BookingHandler.cancelBooking(event, context);
  if (path && path.startsWith('/bookings/') && method === 'GET') return BookingHandler.getBooking(event, context);

  return {
    statusCode: 404,
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ error: { code: 'NOT_FOUND', message: 'Resource not found' } })
  };
};
