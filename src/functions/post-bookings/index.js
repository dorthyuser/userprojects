const crypto = require("crypto");
const { getDatabase } = require("../../helpers/mongodb/connection");
const {
  jsonResponse,
  errorResponse,
} = require("../../helpers/common/response");
const { validateBookingBody } = require("../../helpers/common/validation");
const { safeLog } = require("../../helpers/common/logger");

function createBookingsFunction(app) {
  app.http("post-bookings", {
    methods: ["POST"],
    authLevel: "anonymous",
    route: "bookings",
    handler: async (request, context) => {
      const start = Date.now();
      try {
        const body = await request.json();
        const { errors, value } = validateBookingBody(body || {});
        if (errors.length)
          return errorResponse(
            400,
            "VALIDATION_ERROR",
            "Invalid request body",
            errors
          );

        const db = await getDatabase();
        const existing = await db
          .collection("booking_idempotency")
          .findOne({ idempotencyKey: value.idempotencyKey });
        if (existing?.bookingId) {
          const booking = await db
            .collection("bookings")
            .findOne(
              { bookingId: existing.bookingId },
              { projection: { _id: 0 } }
            );
          if (booking) return jsonResponse(200, booking);
          return errorResponse(409, "CONFLICT", "Duplicate idempotency key");
        }

        const hotel = await db
          .collection("hotels")
          .findOne({ hotelId: value.hotelId, isActive: true });
        if (!hotel) return errorResponse(404, "NOT_FOUND", "Hotel not found");

        const inventory = await db
          .collection("hotel_room_inventory")
          .findOne({ hotelId: value.hotelId, roomType: value.roomType });
        if (
          !inventory ||
          inventory.availableRooms < value.rooms ||
          inventory.occupancyLimit < value.guests
        )
          return errorResponse(409, "CONFLICT", "No inventory available");

        const bookingId = `b_${crypto.randomBytes(4).toString("hex")}`;
        const nights = Math.max(
          1,
          Math.ceil(
            (new Date(value.checkOutDate) - new Date(value.checkInDate)) /
              86400000
          )
        );
        const totalAmount = Number(
          (Number(inventory.nightlyRate) * nights * value.rooms).toFixed(2)
        );
        const now = new Date().toISOString();

        await db
          .collection("booking_idempotency")
          .insertOne({
            idempotencyKey: value.idempotencyKey,
            bookingId,
            requestHash: crypto
              .createHash("sha256")
              .update(JSON.stringify(value))
              .digest("hex"),
            status: "COMPLETED",
            createdAt: new Date(),
            updatedAt: new Date(),
          });
        await db
          .collection("hotel_room_inventory")
          .updateOne(
            { inventoryId: inventory.inventoryId },
            {
              $inc: { availableRooms: -value.rooms },
              $set: { updatedAt: new Date() },
            }
          );
        await db
          .collection("bookings")
          .insertOne({
            bookingId,
            hotelId: value.hotelId,
            roomType: value.roomType,
            guestName: value.guestName,
            guestEmail: value.guestEmail,
            guestPhone: value.guestPhone || null,
            checkInDate: value.checkInDate,
            checkOutDate: value.checkOutDate,
            rooms: value.rooms,
            guests: value.guests,
            totalAmount,
            currency: inventory.currency,
            status: "CONFIRMED",
            specialRequests: value.specialRequests || null,
            cancelledAt: null,
            cancelReason: null,
            createdAt: new Date(now),
            updatedAt: new Date(now),
          });

        safeLog(context, "Booking created", {
          durationMs: Date.now() - start,
          bookingId,
          hotelId: value.hotelId,
          guestEmail: value.guestEmail,
        });
        return jsonResponse(201, {
          bookingId,
          status: "CONFIRMED",
          hotelId: value.hotelId,
          guestName: value.guestName,
          guestEmail: value.guestEmail,
          checkInDate: value.checkInDate,
          checkOutDate: value.checkOutDate,
          roomType: value.roomType,
          rooms: value.rooms,
          guests: value.guests,
          totalAmount,
          currency: inventory.currency,
          createdAt: now,
        });
      } catch {
        return errorResponse(
          500,
          "SERVER_ERROR",
          "Booking could not be created"
        );
      }
    },
  });
}

module.exports = { createBookingsFunction };
