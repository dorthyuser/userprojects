const { app } = require("@azure/functions");
const { createHotelsFunction } = require("./functions/get-hotels");
const { createBookingsFunction } = require("./functions/post-bookings");
const { createBookingByIdFunction } = require("./functions/get-booking-by-id");
const {
  createCancelBookingFunction,
} = require("./functions/post-booking-cancel");

createHotelsFunction(app);
createBookingsFunction(app);
createBookingByIdFunction(app);
createCancelBookingFunction(app);
