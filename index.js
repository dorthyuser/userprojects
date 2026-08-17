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

app.use((req, res, next) => {
  try {
    req.correlation_id = req.headers['x-correlation-id'] || require('crypto').randomUUID();
    next();
  } catch (error) {
    console.log(' Failed', error && error.message ? error.message : error);
    res.status(500).json({ error: { code: 'SERVER_ERROR', message: 'Unexpected failure', details: {} } });
  }
});

app.get('/health', (req, res) => {
  try {
    console.log(' Connected');
    res.status(200).json({ status: 'ok' });
  } catch (error) {
    console.log(' Failed', error && error.message ? error.message : error);
    res.status(500).json({ error: { code: 'SERVER_ERROR', message: 'Unexpected failure', details: {} } });
  }
});

app.use('/', routes);

app.use((req, res) => {
  try {
    res.status(404).json({ error: { code: 'NOT_FOUND', message: 'Resource not found', details: {} } });
  } catch (error) {
    console.log(' Failed', error && error.message ? error.message : error);
  }
});

app.use((error, req, res, next) => {
  try {
    console.log(' Failed', error && error.message ? error.message : error);
    res.status(500).json({ error: { code: 'SERVER_ERROR', message: 'Unexpected failure', details: {} } });
  } catch (e) {
    console.log(' Failed', e && e.message ? e.message : e);
  }
});

app.listen(PORT, () => {
  try {
    console.log(' Connected');
  } catch (error) {
    console.log(' Failed', error && error.message ? error.message : error);
  }
});
