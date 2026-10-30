const express = require('express');
const cors = require('cors');
const helmet = require('helmet');
const pinoHttp = require('pino-http');
const { v4: uuidv4 } = require('uuid');
const { createLogger } = require('./utils/logger');
const { createDb } = require('./db');
const { createOrdersRouter } = require('./routes/orders');
const { errorHandler, notFoundHandler } = require('./middleware/errorHandler');

function createApp({ logger = createLogger() } = {}) {
  const app = express();
  const db = createDb({ logger });

  app.disable('x-powered-by');
  app.use(helmet());
  app.use(cors({ origin: '*', methods: ['GET', 'POST', 'DELETE', 'OPTIONS'] }));
  app.use(express.json({ limit: '1mb' }));

  app.use((req, res, next) => {
    const correlationId = req.header('x-correlation-id') || `corr-${uuidv4()}`;
    req.correlationId = correlationId;
    res.setHeader('x-correlation-id', correlationId);
    next();
  });

  app.use(pinoHttp({
    logger,
    genReqId: (req) => req.correlationId,
    customProps: (req) => ({ correlationId: req.correlationId }),
    serializers: {
      req(req) {
        return {
          method: req.method,
          url: req.url,
          correlationId: req.correlationId
        };
      },
      res(res) {
        return { statusCode: res.statusCode };
      }
    }
  }));

  app.get('/health', (req, res) => {
    res.status(200).json({ status: 'ok' });
  });

  app.use('/api/orders', createOrdersRouter({ db, logger }));
  app.use(notFoundHandler);
  app.use(errorHandler(logger));

  return app;
}

module.exports = { createApp };
