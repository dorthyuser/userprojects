const { expect } = require('chai');
const request = require('supertest');
const express = require('express');

// We require the router from the project. The router module is an IIFE that
// returns an express.Router instance, so requiring it here is sufficient.
const router = require('../routes');

// Helper to create an express app that mounts the router under /convert
function createApp() {
 const app = express();
 app.use(express.json());
 app.use('/convert', router);
 return app;
}

describe('POST /convert router', () => {
 const ORIGINAL_RATE = process.env.RATE_USD_INR;
 const ORIGINAL_BACKEND_RATE = process.env.BACKEND_RATE_USD_INR;

 after(() => {
 // restore environment variables after the whole suite
 if (typeof ORIGINAL_RATE === 'undefined') delete process.env.RATE_USD_INR; else process.env.RATE_USD_INR = ORIGINAL_RATE;
 if (typeof ORIGINAL_BACKEND_RATE === 'undefined') delete process.env.BACKEND_RATE_USD_INR; else process.env.BACKEND_RATE_USD_INR = ORIGINAL_BACKEND_RATE;
 });

 beforeEach(() => {
 // default rate used by tests unless explicitly changed in a test
 process.env.RATE_USD_INR = '82.5';
 delete process.env.BACKEND_RATE_USD_INR;
 });

 it('converts USD to INR using JSON body and returns proper structure', async () => {
 const app = createApp();

 const res = await request(app)
 .post('/convert')
 .send({ from: 'USD', to: 'INR', amount: 100 })
 .expect(200);

 expect(res.body).to.have.property('amount', 100);
 expect(res.body).to.have.property('from', 'USD');
 expect(res.body).to.have.property('to', 'INR');
 expect(res.body).to.have.property('rate');
 expect(res.body.rate).to.equal(parseFloat(process.env.RATE_USD_INR));
 expect(res.body).to.have.property('converted');
 expect(res.body.converted).to.equal(100 * parseFloat(process.env.RATE_USD_INR));
 // timestamp should be a valid ISO string
 expect(new Date(res.body.timestamp).toISOString()).to.equal(res.body.timestamp);
 });

 it('accepts query parameters for conversion', async () => {
 const app = createApp();

 const res = await request(app)
 .post('/convert?from=USD&to=INR&amount=50')
 .expect(200);

 expect(res.body.amount).to.equal(50);
 expect(res.body.rate).to.equal(parseFloat(process.env.RATE_USD_INR));
 expect(res.body.converted).to.equal(50 * parseFloat(process.env.RATE_USD_INR));
 });

 it('converts INR to USD using reciprocal of env rate', async () => {
 const app = createApp();

 // With RATE_USD_INR = 82.5, converting 8250 INR should return 100 USD
 const res = await request(app)
 .post('/convert')
 .send({ from: 'INR', to: 'USD', amount: 8250 })
 .expect(200);

 const envRate = parseFloat(process.env.RATE_USD_INR);
 const expectedRate = envRate ? 1 / envRate : 1;

 expect(res.body.from).to.equal('INR');
 expect(res.body.to).to.equal('USD');
 expect(res.body.rate).to.equal(expectedRate);
 // use a close comparison for floating calculations
 expect(res.body.converted).to.be.closeTo(8250 * expectedRate, 1e-9);
 });

 it('returns rate 1 and converted equal to amount when from and to are same', async () => {
 const app = createApp();

 const res = await request(app)
 .post('/convert')
 .send({ from: 'USD', to: 'USD', amount: 12.34 })
 .expect(200);

 expect(res.body.rate).to.equal(1);
 expect(res.body.converted).to.equal(12.34);
 });

 it('returns 400 for invalid non-numeric amount (string)', async () => {
 const app = createApp();

 const res = await request(app)
 .post('/convert')
 .send({ from: 'USD', to: 'INR', amount: 'abc' })
 .expect(400);

 expect(res.body).to.have.property('error', 'Invalid amount');
 });

 it('returns 400 when amount is missing', async () => {
 const app = createApp();

 const res = await request(app)
 .post('/convert')
 .send({ from: 'USD', to: 'INR' })
 .expect(400);

 expect(res.body).to.have.property('error', 'Invalid amount');
 });

 it('returns 400 for unsupported currency conversion', async () => {
 const app = createApp();

 const res = await request(app)
 .post('/convert')
 .send({ from: 'EUR', to: 'INR', amount: 10 })
 .expect(400);

 expect(res.body).to.have.property('error', 'Conversion not supported');
 });

 it('uses BACKEND_RATE_USD_INR when RATE_USD_INR is not set', async () => {
 // Remove RATE_USD_INR and set BACKEND_RATE_USD_INR
 delete process.env.RATE_USD_INR;
 process.env.BACKEND_RATE_USD_INR = '100';

 const app = createApp();

 const res = await request(app)
 .post('/convert')
 .send({ from: 'USD', to: 'INR', amount: 2 })
 .expect(200);

 expect(res.body.rate).to.equal(100);
 expect(res.body.converted).to.equal(200);
 });

 it('falls back to default rate and uses rate=1 for INR->USD when env rate is invalid', async () => {
 // Set an invalid RATE_USD_INR so parseFloat yields NaN
 process.env.RATE_USD_INR = 'not-a-number';
 delete process.env.BACKEND_RATE_USD_INR;

 const app = createApp();

 const res = await request(app)
 .post('/convert')
 .send({ from: 'INR', to: 'USD', amount: 100 })
 .expect(200);

 // According to code, envRate is falsy -> rate becomes 1 for INR->USD
 expect(res.body.rate).to.equal(1);
 expect(res.body.converted).to.equal(100);
 });

 it('accepts numeric amounts provided as strings', async () => {
 const app = createApp();

 const res = await request(app)
 .post('/convert')
 .send({ from: 'USD', to: 'INR', amount: '123.45' })
 .expect(200);

 expect(res.body.amount).to.equal(123.45);
 expect(res.body.converted).to.equal(123.45 * parseFloat(process.env.RATE_USD_INR));
 });
});
