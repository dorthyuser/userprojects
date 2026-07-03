const crypto = require('crypto');

function requestIdMiddleware(req, res, next) {
  try {
    const requestId = req.headers['x-request-id'] || crypto.randomUUID();
    res.locals.requestId = requestId;
    res.setHeader('X-Request-Id', requestId);
    console.log(' Connected');
    next();
  } catch (error) {
    console.error(' Failed', error && error.message ? error.message : error);
    next(error);
  }
}

function notFoundHandler(req, res) {
  try {
    console.log(' Connected');
    res.status(404).json({ error: { code: 'NOT_FOUND', message: 'Route not found', requestId: res.locals.requestId || null } });
  } catch (error) {
    console.error(' Failed', error && error.message ? error.message : error);
    res.status(500).json({ error: { code: 'INTERNAL_ERROR', message: 'An unexpected error occurred', requestId: res.locals.requestId || null } });
  }
}

function errorHandler(error, req, res, next) {
  try {
    console.error(' Failed', error && error.message ? error.message : error);
    const statusCode = error && error.details ? 400 : 500;
    res.status(statusCode).json({ error: { code: statusCode === 400 ? 'VALIDATION_ERROR' : 'INTERNAL_ERROR', message: statusCode === 400 ? error.message : 'An unexpected error occurred', details: statusCode === 400 ? error.details || [] : undefined, requestId: res.locals.requestId || null } });
  } catch (handlerError) {
    console.error(' Failed', handlerError && handlerError.message ? handlerError.message : handlerError);
    next(handlerError);
  }
}

module.exports = { requestIdMiddleware, notFoundHandler, errorHandler };