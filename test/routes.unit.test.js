const { expect } = require('chai');
const request = require('supertest');
const express = require('express');

const router = require('../routes');

function createApp() {
  const app = express();
  app.use(express.json());
  app.use('/convert', router);
  return app;
}

describe('POST /convert router - additional unit tests', () => {
  const ORIGINAL_RATE_USD_INR = process.env.RATE_USD_INR;
  const ORIGINAL_BACKEND_RATE_USD_INR = process.env.BACKEND_RATE_USD_INR;
  const ORIGINAL_RATE_USD_AUD = process.env.RATE_USD_AUD;
  const ORIGINAL_BACKEND_RATE_USD_AUD = process.env.BACKEND_RATE_USD_AUD;

  after(() => {
    // Restore environment
    if (typeof ORIGINAL_RATE_USD_INR === 'undefined') delete process.env.RATE_USD_INR; else process.env.RATE_USD_INR = ORIGINAL_RATE_USD_INR;
    if (typeof ORIGINAL_BACKEND_RATE_USD_INR === 'undefined') delete process.env.BACKEND_RATE_USD_INR; else process.env.BACKEND_RATE_USD_INR = ORIGINAL_BACKEND_RATE_USD_INR;
    if (typeof ORIGINAL_RATE_USD_AUD === 'undefined') delete process.env.RATE_USD_AUD; else process.env.RATE_USD_AUD = ORIGINAL_RATE_USD_AUD;
    if (typeof ORIGINAL_BACKEND_RATE_USD_AUD === 'undefined') delete process.env.BACKEND_RATE_USD_AUD; else process.env.BACKEND_RATE_USD_AUD = ORIGINAL_BACKEND_RATE_USD_AUD;
  });

  beforeEach(() => {
    // Provide default safe values
    process.env.RATE_USD_INR = '82.5';
    delete process.env.BACKEND_RATE_USD_INR;
    delete process.env.RATE_USD_AUD;
    delete process.env.BACKEND_RATE_USD_AUD;
  });

  it('converts USD to AUD when RATE_USD_AUD is set', async () => {
    process.env.RATE_USD_AUD = '1.6';
    const app = createApp();

    const res = await request(app)
      .post('/convert')
      .send({ from: 'USD', to: 'AUD', amount: 10 })
      .expect(200);

    expect(res.body.rate).to.equal(1.6);
    expect(res.body.converted).to.equal(16);
  });

  it('converts AUD to USD using reciprocal of RATE_USD_AUD', async () => {
    process.env.RATE_USD_AUD = '1.5';
    const app = createApp();

    const res = await request(app)
      .post('/convert')
      .send({ from: 'AUD', to: 'USD', amount: 15 })
      .expect(200);

    expect(res.body.rate).to.equal(1 / 1.5);
    expect(res.body.converted).to.be.closeTo(15 * (1 / 1.5), 1e-9);
  });

  it('converts AUD to INR via USD when both rates are present', async () => {
    process.env.RATE_USD_AUD = '1.5';
    process.env.RATE_USD_INR = '82.5';

    const app = createApp();

    // AUD -> USD -> INR => rate = (1 / RATE_USD_AUD) * RATE_USD_INR
    const expectedRate = (1 / parseFloat(process.env.RATE_USD_AUD)) * parseFloat(process.env.RATE_USD_INR);

    const res = await request(app)
      .post('/convert')
      .send({ from: 'AUD', to: 'INR', amount: 2 })
      .expect(200);

    expect(res.body.rate).to.equal(expectedRate);
    expect(res.body.converted).to.equal(2 * expectedRate);
  });

  it('returns 400 for INR -> AUD if intermediate rates are missing or invalid', async () => {
    // set RATE_USD_INR invalid (NaN) and no AUD rate
    process.env.RATE_USD_INR = 'not-a-number';
    delete process.env.RATE_USD_AUD;

    const app = createApp();

    const res = await request(app)
      .post('/convert')
      .send({ from: 'INR', to: 'AUD', amount: 100 })
      .expect(400);

    expect(res.body).to.have.property('error', 'Conversion not supported');
  });

  it('query parameters override JSON body when both are provided', async () => {
    process.env.RATE_USD_INR = '82.5';
    const app = createApp();

    const res = await request(app)
      .post('/convert?from=USD&to=INR&amount=20')
      .send({ from: 'USD', to: 'INR', amount: 100 })
      .expect(200);

    // query amount 20 should be used
    expect(res.body.amount).to.equal(20);
    expect(res.body.converted).to.equal(20 * parseFloat(process.env.RATE_USD_INR));
  });

  it('accepts zero and negative amounts and converts correctly', async () => {
    const app = createApp();

    const zeroRes = await request(app)
      .post('/convert')
      .send({ from: 'USD', to: 'INR', amount: 0 })
      .expect(200);
    expect(zeroRes.body.converted).to.equal(0);

    const negRes = await request(app)
      .post('/convert')
      .send({ from: 'USD', to: 'INR', amount: -10 })
      .expect(200);
    expect(negRes.body.converted).to.equal(-10 * parseFloat(process.env.RATE_USD_INR));
  });

  it('parses numeric amounts provided as query strings with whitespace', async () => {
    const app = createApp();

    const res = await request(app)
      .post('/convert?from=USD&to=INR&amount=%20%20123.4%20')
      .expect(200);

    expect(res.body.amount).to.equal(123.4);
    expect(res.body.converted).to.equal(123.4 * parseFloat(process.env.RATE_USD_INR));
  });

  it('currency codes are case-insensitive', async () => {
    const app = createApp();

    const res = await request(app)
      .post('/convert')
      .send({ from: 'usd', to: 'inr', amount: 5 })
      .expect(200);

    expect(res.body.from).to.equal('USD');
    expect(res.body.to).to.equal('INR');
    expect(res.body.converted).to.equal(5 * parseFloat(process.env.RATE_USD_INR));
  });

  it('handles very large amounts without throwing and produces expected numeric result', async () => {
    const app = createApp();
    const big = 1e12; // 1 trillion

    const res = await request(app)
      .post('/convert')
      .send({ from: 'USD', to: 'INR', amount: big })
      .expect(200);

    expect(res.body.amount).to.equal(big);
    expect(res.body.converted).to.equal(big * parseFloat(process.env.RATE_USD_INR));
  });

  it('timestamp is a valid ISO string and is near the current time', async () => {
    const app = createApp();

    const before = Date.now();
    const res = await request(app)
      .post('/convert')
      .send({ from: 'USD', to: 'INR', amount: 1 })
      .expect(200);
    const after = Date.now();

    const ts = Date.parse(res.body.timestamp);
    expect(Number.isFinite(ts)).to.be.true;
    // timestamp should be between before and after (allowing some small slack)
    expect(ts).to.be.at.least(before - 1000);
    expect(ts).to.be.at.most(after + 1000);
  });
});
