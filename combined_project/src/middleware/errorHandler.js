const { AppError } = require('../utils/errors');

function notFoundHandler(req, res) {
  res.status(404).json({
    error: {
      code: 'NOT_FOUND',
      message: 'Route not found',
      correlationId: req.correlationId || null
    }
  });
}

function errorHandler(logger) {
  return (err, req, res, next) => {
    const correlationId = req.correlationId || null;
    const statusCode = err instanceof AppError ? err.statusCode : 500;
    const code = err instanceof AppError ? err.code : 'INTERNAL_ERROR';
    const message = err instanceof AppError ? err.publicMessage : 'Unexpected error';

    logger.error({
      correlationId,
      code,
      statusCode,
      err: { message: err.message, name: err.name }
    }, 'request_failed');

    res.status(statusCode).json({
      error: {
        code,
        message,
        correlationId
      }
    });
  };
}

module.exports = { errorHandler, notFoundHandler };
