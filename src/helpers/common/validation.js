function isIsoDate(value) {
  return (
    typeof value === "string" &&
    /^\d{4}-\d{2}-\d{2}$/.test(value) &&
    !Number.isNaN(Date.parse(value))
  );
}

function normalizeString(value) {
  return typeof value === "string" ? value.trim() : value;
}

function validateHotelsQuery(query) {
  const errors = [];
  const city = normalizeString(query.city);
  const checkInDate = normalizeString(query.checkInDate);
  const checkOutDate = normalizeString(query.checkOutDate);
  const guests = Number(query.guests);
  const page = query.page ? Number(query.page) : 1;
  const pageSize = query.pageSize
    ? Number(query.pageSize)
    : Number(process.env.DEFAULT_PAGE_SIZE || 20);
  const maxPageSize = Number(process.env.MAX_PAGE_SIZE || 100);

  if (!city) errors.push({ field: "city", message: "city is required" });
  if (!isIsoDate(checkInDate))
    errors.push({
      field: "checkInDate",
      message: "checkInDate must be a valid YYYY-MM-DD date",
    });
  if (!isIsoDate(checkOutDate))
    errors.push({
      field: "checkOutDate",
      message: "checkOutDate must be a valid YYYY-MM-DD date",
    });
  if (!Number.isInteger(guests) || guests < 1)
    errors.push({
      field: "guests",
      message: "guests must be a positive integer",
    });
  if (!Number.isInteger(page) || page < 1)
    errors.push({ field: "page", message: "page must be a positive integer" });
  if (!Number.isInteger(pageSize) || pageSize < 1 || pageSize > maxPageSize)
    errors.push({
      field: "pageSize",
      message: `pageSize must be between 1 and ${maxPageSize}`,
    });
  if (
    isIsoDate(checkInDate) &&
    isIsoDate(checkOutDate) &&
    new Date(checkOutDate) <= new Date(checkInDate)
  )
    errors.push({
      field: "checkOutDate",
      message: "checkOutDate must be later than checkInDate",
    });

  return {
    errors,
    value: { city, checkInDate, checkOutDate, guests, page, pageSize },
  };
}

function validateBookingBody(body) {
  const errors = [];
  const hotelId = normalizeString(body.hotelId);
  const guestName = normalizeString(body.guestName);
  const guestEmail = normalizeString(body.guestEmail)?.toLowerCase();
  const guestPhone = normalizeString(body.guestPhone);
  const checkInDate = normalizeString(body.checkInDate);
  const checkOutDate = normalizeString(body.checkOutDate);
  const roomType = normalizeString(body.roomType);
  const rooms = Number(body.rooms);
  const guests = Number(body.guests);
  const specialRequests = normalizeString(body.specialRequests);
  const idempotencyKey = normalizeString(body.idempotencyKey);

  if (!hotelId)
    errors.push({ field: "hotelId", message: "hotelId is required" });
  if (!guestName || guestName.length > 120)
    errors.push({
      field: "guestName",
      message: "guestName must be 1-120 characters",
    });
  if (
    !guestEmail ||
    guestEmail.length > 254 ||
    !/^\S+@\S+\.\S+$/.test(guestEmail)
  )
    errors.push({
      field: "guestEmail",
      message: "guestEmail must be a valid email address",
    });
  if (guestPhone && !/^\+[1-9]\d{1,14}$/.test(guestPhone))
    errors.push({
      field: "guestPhone",
      message: "guestPhone must be in E.164 format",
    });
  if (!isIsoDate(checkInDate))
    errors.push({
      field: "checkInDate",
      message: "checkInDate must be a valid YYYY-MM-DD date",
    });
  if (!isIsoDate(checkOutDate))
    errors.push({
      field: "checkOutDate",
      message: "checkOutDate must be a valid YYYY-MM-DD date",
    });
  if (!roomType)
    errors.push({ field: "roomType", message: "roomType is required" });
  if (!Number.isInteger(rooms) || rooms < 1)
    errors.push({
      field: "rooms",
      message: "rooms must be a positive integer",
    });
  if (!Number.isInteger(guests) || guests < 1)
    errors.push({
      field: "guests",
      message: "guests must be a positive integer",
    });
  if (specialRequests && specialRequests.length > 1000)
    errors.push({
      field: "specialRequests",
      message: "specialRequests must be 1000 characters or less",
    });
  if (!idempotencyKey || idempotencyKey.length > 64)
    errors.push({
      field: "idempotencyKey",
      message: "idempotencyKey is required and must be 64 characters or less",
    });
  if (
    isIsoDate(checkInDate) &&
    isIsoDate(checkOutDate) &&
    new Date(checkOutDate) <= new Date(checkInDate)
  )
    errors.push({
      field: "checkOutDate",
      message: "checkOutDate must be later than checkInDate",
    });

  return {
    errors,
    value: {
      hotelId,
      guestName,
      guestEmail,
      guestPhone,
      checkInDate,
      checkOutDate,
      roomType,
      rooms,
      guests,
      specialRequests,
      idempotencyKey,
    },
  };
}

function validateBookingId(bookingId) {
  if (
    !bookingId ||
    typeof bookingId !== "string" ||
    !/^b_[A-Za-z0-9]+$/.test(bookingId)
  ) {
    return {
      errors: [{ field: "bookingId", message: "bookingId is invalid" }],
    };
  }
  return { errors: [], value: bookingId };
}

function validateCancelBody(body) {
  const reason = normalizeString(body.reason);
  const errors = [];
  if (reason && reason.length > 500)
    errors.push({
      field: "reason",
      message: "reason must be 500 characters or less",
    });
  return { errors, value: { reason: reason || "" } };
}

module.exports = {
  validateHotelsQuery,
  validateBookingBody,
  validateBookingId,
  validateCancelBody,
  normalizeString,
};
