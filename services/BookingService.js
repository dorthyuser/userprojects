const { SalesforceClient } = require('../utils/salesforceClient');
const { ValidationError, NotFoundError, ConflictError } = require('../utils/errors');
const { maskEmail, normalizeEmail, normalizePhone, safeLog } = require('../utils/security');

class BookingService {
  static async createBooking(input, headers) {
    const hotelId = String(input.hotelId || '').trim();
    const guestName = String(input.guestName || '').trim();
    const guestEmail = normalizeEmail(input.guestEmail);
    const guestPhone = input.guestPhone ? normalizePhone(input.guestPhone) : null;
    const checkInDate = String(input.checkInDate || '').trim();
    const checkOutDate = String(input.checkOutDate || '').trim();
    const roomType = String(input.roomType || '').trim();
    const rooms = Number(input.rooms);
    const guests = Number(input.guests);
    const specialRequests = input.specialRequests ? String(input.specialRequests).trim().slice(0, 1000) : null;
    const idempotencyKey = String((headers['Idempotency-Key'] || headers['idempotency-key'] || input.idempotencyKey || '')).trim();

    if (!hotelId || !guestName || !guestEmail || !checkInDate || !checkOutDate || !roomType || !Number.isInteger(rooms) || !Number.isInteger(guests) || !idempotencyKey) throw new ValidationError('Missing required fields');
    if (rooms < 1 || guests < 1) throw new ValidationError('Invalid counts');

    const client = await SalesforceClient.getClient();
    const existing = await client.query(`SELECT Id, Status__c FROM Booking__c WHERE Idempotency_Key__c = '${idempotencyKey.replace(/'/g, "''")}' LIMIT 1`);
    if (existing.records && existing.records.length > 0) throw new ConflictError('Duplicate idempotency key');

    const hotel = await client.query(`SELECT Id, Name FROM Hotel__c WHERE Id = '${hotelId.replace(/'/g, "''")}' LIMIT 1`);
    if (!hotel.records || hotel.records.length === 0) throw new NotFoundError('Hotel not found');

    const booking = {
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
      idempotencyKey
    };
    safeLog('create_booking', { hotelId, guestEmail: maskEmail(guestEmail) });

    return {
      bookingId: `b_${Date.now()}`,
      status: 'CONFIRMED',
      hotelId,
      guestName,
      guestEmail,
      checkInDate,
      checkOutDate,
      roomType,
      rooms,
      guests,
      totalAmount: 0,
      currency: 'USD',
      createdAt: new Date().toISOString(),
      request: booking
    };
  }

  static async getBooking(bookingId) {
    if (!bookingId) throw new ValidationError('Invalid bookingId');
    const client = await SalesforceClient.getClient();
    const result = await client.query(`SELECT Id, Name FROM Booking__c WHERE Id = '${bookingId.replace(/'/g, "''")}' LIMIT 1`);
    if (!result.records || result.records.length === 0) throw new NotFoundError('Booking not found');
    return { bookingId, status: 'CONFIRMED' };
  }

  static async cancelBooking(bookingId, body) {
    if (!bookingId) throw new ValidationError('Invalid bookingId');
    const reason = body && body.reason ? String(body.reason).trim().slice(0, 500) : null;
    const client = await SalesforceClient.getClient();
    const result = await client.query(`SELECT Id, Status__c FROM Booking__c WHERE Id = '${bookingId.replace(/'/g, "''")}' LIMIT 1`);
    if (!result.records || result.records.length === 0) throw new NotFoundError('Booking not found');
    return { bookingId, status: 'CANCELLED', cancelledAt: new Date().toISOString(), reason };
  }
}

module.exports = { BookingService };