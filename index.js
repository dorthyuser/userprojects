const express = require('express');
const cors = require('cors');
const dotenv = require('dotenv');

dotenv.config();

const app = express();
const routes = require('./routes');

try {
  require('./connections/mysql');
  console.log('mysql Connected');
} catch (error) {
  console.error('mysql Failed', {
    message: error.message,
    code: error.code || 'CONNECTION_INIT_ERROR'
  });
}

app.use(cors({ origin: '*', methods: ['GET', 'POST', 'PUT', 'PATCH', 'DELETE', 'OPTIONS'], allowedHeaders: ['Content-Type', 'Authorization', 'X-Request-Id'] }));
app.use(express.json({ limit: '1mb' }));
app.use(express.urlencoded({ extended: true }));

app.use((req, res, next) => {
  try {
    req.requestId = req.headers['x-request-id'] || `req_${Date.now()}`;
    next();
  } catch (error) {
    console.error('request-middleware Failed', {
      requestId: req.headers['x-request-id'] || null,
      message: error.message,
      code: 'REQUEST_MIDDLEWARE_ERROR'
    });
    return res.status(500).json({
      error: {
        code: 'REQUEST_MIDDLEWARE_ERROR',
        message: 'Internal server error',
        requestId: req.headers['x-request-id'] || null
      }
    });
  }
});

app.use('/', routes);

app.use((req, res) => {
  try {
    return res.status(404).json({
      error: {
        code: 'RESOURCE_NOT_FOUND',
        message: 'Requested resource not found',
        requestId: req.requestId || null
      }
    });
  } catch (error) {
    console.error('404-handler Failed', {
      requestId: req.requestId || null,
      message: error.message,
      code: 'NOT_FOUND_HANDLER_ERROR'
    });
    return res.status(500).json({
      error: {
        code: 'NOT_FOUND_HANDLER_ERROR',
        message: 'Internal server error',
        requestId: req.requestId || null
      }
    });
  }
});

app.use((error, req, res, next) => {
  try {
    console.error('global-error-handler Failed', {
      requestId: req.requestId || null,
      message: error.message,
      code: error.code || 'INTERNAL_SERVER_ERROR'
    });
    return res.status(error.statusCode || 500).json({
      error: {
        code: error.code || 'INTERNAL_SERVER_ERROR',
        message: error.publicMessage || 'Internal server error',
        requestId: req.requestId || null
      }
    });
  } catch (handlerError) {
    console.error('global-error-handler Failed', {
      requestId: req && req.requestId ? req.requestId : null,
      message: handlerError.message,
      code: 'GLOBAL_ERROR_HANDLER_FAILURE'
    });
    return res.status(500).json({
      error: {
        code: 'GLOBAL_ERROR_HANDLER_FAILURE',
        message: 'Internal server error',
        requestId: req && req.requestId ? req.requestId : null
      }
    });
  }
});

const PORT = 8080;

try {
  app.listen(PORT, () => {
    console.log('server Connected');
  });
} catch (error) {
  console.error('server Failed', {
    message: error.message,
    code: 'SERVER_START_ERROR'
  });
}
