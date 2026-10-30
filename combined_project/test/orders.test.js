const request = require('supertest');
const express = require('express');
const { createOrdersRouter } = require('../src/routes/orders');

function createMockDb() {
  return {
    query: jest.fn(),
    transaction: jest.fn(async (work) => work({ query: jest.fn() }))
  };
}

describe('orders api', () => {
  test('GET /api/orders returns data', async () => {
    const db = createMockDb();
    db.query.mockResolvedValue({ rows: [{ id: 1, account_id: 2, order_data: { items: [], total: 1, status: 'pending' }, created_at: new Date(), name: 'John', email: 'john@example.com', address: '123 Street' }] });
    const app = express();
    app.use(express.json());
    app.use('/api/orders', createOrdersRouter({ db, logger: { info: jest.fn(), error: jest.fn() } }));

    const res = await request(app).get('/api/orders');
    expect(res.status).toBe(200);
    expect(res.body.data).toHaveLength(1);
  });

  test('POST /api/orders validates payload', async () => {
    const db = createMockDb();
    const app = express();
    app.use(express.json());
    app.use('/api/orders', createOrdersRouter({ db, logger: { info: jest.fn(), error: jest.fn() } }));

    const res = await request(app).post('/api/orders').send({ account_id: 0, order_data: {} });
    expect(res.status).toBe(400);
    expect(res.body.error.code).toBe('VALIDATION_ERROR');
  });
});
