const express = require('express');
const cors = require('cors');
const dotenv = require('dotenv');
const routes = require('./routes');

dotenv.config();

const app = express();
const PORT = process.env.PORT || 8080;

app.use(cors({ origin: '*', methods: ['GET', 'POST', 'PUT', 'PATCH', 'DELETE', 'OPTIONS'] }));
app.use(express.json({ limit: '1mb' }));
app.use(express.urlencoded({ extended: true }));

app.use('/', routes);

app.use((err, req, res, next) => {
  try {
    console.error(' Failed', { message: err && err.message ? err.message : 'Internal error', requestId: req.headers['x-request-id'] || null });
    res.status(500).json({ error: { code: 'INTERNAL_SERVER_ERROR', message: 'An internal error occurred.' } });
  } catch (e) {
    console.error(' Failed', { message: 'Unhandled error in error middleware' });
    res.status(500).json({ error: { code: 'INTERNAL_SERVER_ERROR', message: 'An internal error occurred.' } });
  }
});

try {
  app.listen(PORT, () => {
    console.log(' Connected');
  });
} catch (error) {
  console.error(' Failed', error && error.message ? error.message : error);
}
