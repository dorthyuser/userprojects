function errorHandler(err, req, res, next) {
  try {
    console.error(' Failed', { message: err.message, code: err.code || 'MIDDLEWARE_ERROR' });
    res.status(err.statusCode || 500).json({
      success: false,
      error: {
        code: err.code || 'INTERNAL_ERROR',
        message: err.publicMessage || 'An unexpected error occurred',
        timestamp: new Date().toISOString()
      }
    });
  } catch (error) {
    console.error(' Failed', { message: error.message, code: error.code || 'MIDDLEWARE_FATAL_ERROR' });
    res.status(500).json({
      success: false,
      error: {
        code: 'INTERNAL_ERROR',
        message: 'An unexpected error occurred',
        timestamp: new Date().toISOString()
      }
    });
  }
}

module.exports = errorHandler;
