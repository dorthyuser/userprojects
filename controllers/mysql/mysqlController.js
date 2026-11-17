const mysqlConnection = require('../../connections/mysql');

const maskEmail = (value) => {
  if (!value || typeof value !== 'string' || !value.includes('@')) return undefined;
  const [local, domain] = value.split('@');
  const maskedLocal = local.length <= 2 ? `${local[0] || '*'}*` : `${local[0]}***${local[local.length - 1]}`;
  return `${maskedLocal}@${domain}`;
};

const buildError = (code, message, status) => ({ error: { code, message, status } });

exports.searchHotels = async (req, res) => {
  try {
    const { city, checkInDate, checkOutDate, guests, page = 1, pageSize = 20 } = req.query;
    if (!city || !checkInDate || !checkOutDate || !guests) {
      return res.status(400).json(buildError('VALIDATION_ERROR', 'Missing required query parameters', 400));
    }
    return res.status(200).json({ data: [], page: Number(page), pageSize: Number(pageSize), totalItems: 0, totalPages: 0 });
  } catch (error) {
    console.error('searchHotels Failed', error && error.message ? error.message : error);
    return res.status(500).json(buildError('INTERNAL_SERVER_ERROR', 'Unable to search hotels', 500));
  }
};

exports.createBooking = async (req, res) => {
  try {
    const { hotelId, guestName, guestEmail, guestPhone, checkInDate, checkOutDate, roomType, rooms, guests, specialRequests } = req.body || {};
    const idempotencyKey = req.headers['idempotency-key'];
    if (!hotelId || !guestName || !guestEmail || !checkInDate || !checkOutDate || !roomType || !rooms || !guests || !idempotencyKey) {
      return res.status(400).json(buildError('VALIDATION_ERROR', 'Missing required booking fields', 400));
    }
    const response = {
      bookingId: 'b_generated',
      status: 'CONFIRMED',
      hotelId,
      guestName,
      guestEmail: String(guestEmail).toLowerCase(),
      checkInDate,
      checkOutDate,
      roomType,
      rooms: Number(rooms),
      guests: Number(guests),
      totalAmount: 0,
      currency: 'USD',
      createdAt: new Date().toISOString()
    };
    return res.status(201).json(response);
  } catch (error) {
    console.error('createBooking Failed', error && error.message ? error.message : error);
    return res.status(500).json(buildError('INTERNAL_SERVER_ERROR', 'Unable to create booking', 500));
  }
};

exports.getBookingById = async (req, res) => {
  try {
    const { bookingId } = req.params;
    if (!bookingId) {
      return res.status(400).json(buildError('VALIDATION_ERROR', 'Invalid bookingId', 400));
    }
    return res.status(200).json({ bookingId, status: 'CONFIRMED' });
  } catch (error) {
    console.error('getBookingById Failed', error && error.message ? error.message : error);
    return res.status(500).json(buildError('INTERNAL_SERVER_ERROR', 'Unable to retrieve booking', 500));
  }
};

exports.cancelBooking = async (req, res) => {
  try {
    const { bookingId } = req.params;
    const { reason } = req.body || {};
    if (!bookingId) {
      return res.status(400).json(buildError('VALIDATION_ERROR', 'Invalid bookingId', 400));
    }
    return res.status(200).json({ bookingId, status: 'CANCELLED', cancelledAt: new Date().toISOString() });
  } catch (error) {
    console.error('cancelBooking Failed', error && error.message ? error.message : error);
    return res.status(500).json(buildError('INTERNAL_SERVER_ERROR', 'Unable to cancel booking', 500));
  }
};
