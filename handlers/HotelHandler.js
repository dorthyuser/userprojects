const { HotelService } = require('../services/HotelService');
const { parseQuery, jsonResponse, handleError } = require('../utils/http');

class HotelHandler {
  static async searchHotels(event) {
    try {
      const query = parseQuery(event.queryStringParameters || {});
      const result = await HotelService.searchHotels(query);
      return jsonResponse(200, result);
    } catch (error) {
      return handleError(error);
    }
  }
}

module.exports = { HotelHandler };