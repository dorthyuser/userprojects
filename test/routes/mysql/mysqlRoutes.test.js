// GENERATED_BY_AI_TEST_ENGINE
const express = require('express');
const request = require('supertest');

jest.mock('../../../controllers/mysql/mysqlController', () => ({
  searchHotels: jest.fn((req, res) => res.status(200).json({ data: [], page: 1, pageSize: 20, totalItems: 0, totalPages: 0 })),
  createBooking: jest.fn((req, res) => res.status(500).json({ error: { code: 'INTERNAL_SERVER_ERROR', message: 'Unable to create booking', status: 500 } })),
  getBookingById: jest.fn((req, res) => res.status(500).json({ error: { code: 'INTERNAL_SERVER_ERROR', message: 'Unable to fetch booking', status: 500 } })),
  cancelBooking: jest.fn((req, res) => res.status(500).json({ error: { code: 'INTERNAL_SERVER_ERROR', message: 'Unable to cancel booking', status: 500 } }))
}));

const mysqlRoutes = require('../../../routes/mysql/mysqlRoutes');
const mysqlController = require('../../../controllers/mysql/mysqlController');

describe('routes/mysql/mysqlRoutes', () => {
  let app;

  beforeEach(() => {
    app = express();
    app.use(express.json());
    app.use('/', mysqlRoutes);
    jest.clearAllMocks();
  });

  it('routes GET /hotels to searchHotels successfully', async () => {
    const res = await request(app)
      .get('/hotels')
      .query({ city: 'Paris', checkInDate: '2026-01-01', checkOutDate: '2026-01-05', guests: '2' });

    const body = res.body;
    expect(res.status).toBe(200);
    expect(body.data).toEqual([]);
    expect(mysqlController.searchHotels).toHaveBeenCalled();
    expect(mysqlController.searchHotels.mock.calls[0][0]).toEqual(expect.any(Object));
    expect(mysqlController.searchHotels.mock.calls[0][1]).toEqual(expect.any(Object));
  });

  it('routes GET /hotels failure path when query is missing and controller returns validation error', async () => {
    mysqlController.searchHotels.mockImplementationOnce((req, res) => res.status(400).json({ error: { code: 'VALIDATION_ERROR', message: 'Missing required query parameters', status: 400 } }));

    const res = await request(app).get('/hotels').query({ city: 'Paris' });

    const body = res.body;
    expect(res.status).toBe(400);
    expect(body.error.code).toBe('VALIDATION_ERROR');
    expect(mysqlController.searchHotels).toHaveBeenCalled();
  });

  it('routes POST /bookings to createBooking and returns failure response', async () => {
    const res = await request(app).post('/bookings').send({ hotelId: 'H1' });

    const body = res.body;
    expect(res.status).toBe(500);
    expect(body.error.code).toBe('INTERNAL_SERVER_ERROR');
    expect(mysqlController.createBooking).toHaveBeenCalled();
    expect(mysqlController.createBooking.mock.calls[0][0]).toEqual(expect.any(Object));
    expect(mysqlController.createBooking.mock.calls[0][1]).toEqual(expect.any(Object));
  });

  it('routes GET /bookings/:bookingId to getBookingById and returns failure response', async () => {
    const res = await request(app).get('/bookings/ABC123');

    const body = res.body;
    expect(res.status).toBe(500);
    expect(body.error.code).toBe('INTERNAL_SERVER_ERROR');
    expect(mysqlController.getBookingById).toHaveBeenCalled();
    expect(mysqlController.getBookingById.mock.calls[0][0]).toEqual(expect.any(Object));
    expect(mysqlController.getBookingById.mock.calls[0][1]).toEqual(expect.any(Object));
  });

  it('routes POST /bookings/:bookingId/cancel to cancelBooking and returns failure response', async () => {
    const res = await request(app).post('/bookings/ABC123/cancel').send({ reason: 'change of plans' });

    const body = res.body;
    expect(res.status).toBe(500);
    expect(body.error.code).toBe('INTERNAL_SERVER_ERROR');
    expect(mysqlController.cancelBooking).toHaveBeenCalled();
    expect(mysqlController.cancelBooking.mock.calls[0][0]).toEqual(expect.any(Object));
    expect(mysqlController.cancelBooking.mock.calls[0][1]).toEqual(expect.any(Object));
  });
});