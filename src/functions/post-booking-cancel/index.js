const { getDatabase } = require("../../helpers/mongodb/connection");
const {
  jsonResponse,
  errorResponse,
} = require("../../helpers/common/response");
const {
  validateBookingId,
  validateCancelBody,
} = require("../../helpers/common/validation");

function createCancelBookingFunction(app) {
  app.http("post-booking-cancel", {
    methods: ["POST"],
    authLevel: "anonymous",
    route: "bookings/{bookingId}/cancel",
    handler: async (request) => {
      try {
        const { bookingId } = request.params;
        const idCheck = validateBookingId(bookingId);
        if (idCheck.errors.length)
          return errorResponse(
            400,
            "VALIDATION_ERROR",
            "Invalid bookingId",
            idCheck.errors
          );

        const body = await request.json().catch(() => ({}));
        const { errors, value } = validateCancelBody(body || {});
        if (errors.length)
          return errorResponse(
            400,
            "VALIDATION_ERROR",
            "Invalid request body",
            errors
          );

        const db = await getDatabase();
        const booking = await db.collection("bookings").findOne({ bookingId });
        if (!booking)
          return errorResponse(404, "NOT_FOUND", "Booking not found");
        if (booking.status === "CANCELLED")
          return jsonResponse(200, {
            bookingId,
            status: "CANCELLED",
            cancelledAt: booking.cancelledAt,
          });
        if (booking.status !== "CONFIRMED")
          return errorResponse(409, "CONFLICT", "Booking not cancellable");

        await db
          .collection("bookings")
          .updateOne(
            { bookingId },
            {
              $set: {
                status: "CANCELLED",
                cancelledAt: new Date().toISOString(),
                cancelReason: value.reason || null,
                updatedAt: new Date().toISOString(),
              },
            }
          );
        await db
          .collection("hotel_room_inventory")
          .updateOne(
            { hotelId: booking.hotelId, roomType: booking.roomType },
            {
              $inc: { availableRooms: booking.rooms },
              $set: { updatedAt: new Date() },
            }
          );

        return jsonResponse(200, {
          bookingId,
          status: "CANCELLED",
          cancelledAt: new Date().toISOString(),
        });
      } catch {
        return errorResponse(500, "SERVER_ERROR", "Cancellation failed");
      }
    },
  });
}

module.exports = { createCancelBookingFunction };
