const { getDatabase } = require("../../helpers/mongodb/connection");
const {
  jsonResponse,
  errorResponse,
} = require("../../helpers/common/response");
const { validateBookingId } = require("../../helpers/common/validation");

function createBookingByIdFunction(app) {
  app.http("get-booking-by-id", {
    methods: ["GET"],
    authLevel: "anonymous",
    route: "bookings/{bookingId}",
    handler: async (request) => {
      try {
        const { bookingId } = request.params;
        const { errors } = validateBookingId(bookingId);
        if (errors.length)
          return errorResponse(
            400,
            "VALIDATION_ERROR",
            "Invalid bookingId",
            errors
          );

        const db = await getDatabase();
        const booking = await db
          .collection("bookings")
          .findOne({ bookingId }, { projection: { _id: 0 } });
        if (!booking)
          return errorResponse(404, "NOT_FOUND", "Booking not found");

        const hotel = await db
          .collection("hotels")
          .findOne(
            { hotelId: booking.hotelId },
            { projection: { _id: 0, hotelName: 1 } }
          );
        return jsonResponse(200, {
          ...booking,
          hotelName: hotel?.hotelName || "",
        });
      } catch {
        return errorResponse(500, "SERVER_ERROR", "Unexpected error");
      }
    },
  });
}

module.exports = { createBookingByIdFunction };
