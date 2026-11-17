// GENERATED_BY_AI_TEST_ENGINE
const request = require('supertest');
const express = require('express');

jest.mock('../../routes/mysql/mysqlRoutes', () => {
  const mockExpress = require('express');
  const mockRouter = mockExpress.Router();
  mockRouter.get('/mocked-route', (req, res) => res.status(200).json({ ok: true }));
  return mockRouter;
});

const router = require('../../routes/index');

describe('routes/index', () => {
  it('mounts mysql routes and serves the delegated route successfully', async () => {
    const app = express();
    app.use('/', router);

    const res = await request(app).get('/mocked-route');

    const body = res.body;
    expect(res.status).toBe(200);
    expect(body.ok).toBe(true);
  });

  it('returns 404 for an unmapped route', async () => {
    const app = express();
    app.use('/', router);

    const res = await request(app).get('/not-found');

    expect(res.status).toBe(404);
  });
});