const express = require('express');
const cors = require('cors');
const dotenv = require('dotenv');
const routes = require('./routes');
const { requestIdMiddleware, errorHandler, notFoundHandler } = require('./middleware');

dotenv.config();

const app = express();
const PORT = process.env.PORT || 8080;

try {
  app.use(cors({ origin: '*', methods: ['GET', 'POST', 'PUT', 'PATCH', 'DELETE', 'OPTIONS'], allowedHeaders: ['Content-Type', 'Accept', 'X-Request-Id'] }));
  app.use(express.json({ limit: '1mb' }));
  app.use(express.urlencoded({ extended: true }));
  app.use(requestIdMiddleware);
  app.use('/api/v1/currency-transfer', routes);
  app.use(notFoundHandler);
  app.use(errorHandler);
  app.listen(PORT, () => {
    console.log(` Connected on port ${PORT}`);
  });
} catch (error) {
  console.error(' Failed', error && error.message ? error.message : error);
}

module.exports = app;