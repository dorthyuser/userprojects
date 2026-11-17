const { BookingService } = require('../services/BookingService');
const { parseBody, jsonResponse, handleError } = require('../utils/http');

class BookingHandler {
  static async createBooking(event) {
    try {
      const body = parseBody(event.body);
      const result = await BookingService.createBooking(body, event.headers || {});
      return jsonResponse(201, result);
    } catch (error) {
      return handleError(error);
    }
  }

  static async getBooking(event) {
    try {
      const bookingId = (event.pathParameters && event.pathParameters.bookingId) || '';
      const result = await BookingService.getBooking(bookingId);
      return jsonResponse(200, result);
    } catch (error) {
      return handleError(error);
    }
  }

  static async cancelBooking(event) {
    try {
      const bookingId = (event.pathParameters && event.pathParameters.bookingId) || '';
      const body = parseBody(event.body);
      const result = await BookingService.cancelBooking(bookingId, body);
      return jsonResponse(200, result);
    } catch (error) {
      return handleError(error);
    }
  }
}

module.exports = { BookingHandler };