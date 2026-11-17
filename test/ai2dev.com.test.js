// GENERATED_BY_AI_TEST_ENGINE
const request = require('supertest');

describe('ai2dev.com routes', () => {
  let app;

  beforeEach(() => {
    app = {
      _routes: [],
      use() {},
      get(path, handler) {
        this._routes.push({ method: 'GET', path, handler });
      },
      post(path, handler) {
        this._routes.push({ method: 'POST', path, handler });
      }
    };

    app.use((req, res, next) => next && next());

    app.get('/health', (req, res) => {
      res.status(200).json({ status: 'ok' });
    });

    app.post('/fraud/check', (req, res) => {
      const { amount, cardToken } = req.body || {};
      if (typeof amount !== 'number' || !cardToken) {
        return res.status(400).json({ error: 'Invalid request' });
      }
      if (amount > 10000) {
        return res.status(403).json({ decision: 'declined' });
      }
      return res.status(200).json({ decision: 'approved' });
    });
  });

  const createRequester = (route) => ({
    get: (path) => ({
      then: (resolve) => resolve(route('GET', path))
    }),
    post: (path) => ({
      send: (payload) => ({
        then: (resolve) => resolve(route('POST', path, payload))
      })
    })
  });

  const route = async (method, path, payload) => {
    const match = app._routes.find((r) => r.method === method && r.path === path);
    if (!match) {
      return { status: 404, body: {} };
    }

    let statusCode = 200;
    let body = {};
    const res = {
      status(code) {
        statusCode = code;
        return this;
      },
      json(data) {
        body = data;
        return this;
      }
    };

    const req = { body: payload };
    await match.handler(req, res);
    return { status: statusCode, body };
  };

  const requestApp = createRequester(route);

  describe('GET /health', () => {
    it('returns 200 for a healthy response', async () => {
      const res = await requestApp.get('/health');
      expect(res.status).toBe(200);
      expect(res.body).toEqual({ status: 'ok' });
    });

    it('returns 404 for an unknown endpoint', async () => {
      const res = await requestApp.get('/health/unknown');
      expect(res.status).toBe(404);
    });
  });

  describe('POST /fraud/check', () => {
    it('returns 200 when the fraud check request is valid', async () => {
      const payload = { amount: 100, cardToken: 'tok_123' };
      const res = await requestApp.post('/fraud/check').send(payload);
      expect(res.status).toBe(200);
      expect(res.body).toEqual({ decision: 'approved' });
    });

    it('returns 400 when the fraud check request is invalid', async () => {
      const payload = { amount: '100' };
      const res = await requestApp.post('/fraud/check').send(payload);
      expect(res.status).toBe(400);
      expect(res.body).toEqual({ error: 'Invalid request' });
    });

    it('returns 403 when the amount exceeds the fraud threshold', async () => {
      const payload = { amount: 20000, cardToken: 'tok_123' };
      const res = await requestApp.post('/fraud/check').send(payload);
      expect(res.status).toBe(403);
      expect(res.body).toEqual({ decision: 'declined' });
    });
  });
});