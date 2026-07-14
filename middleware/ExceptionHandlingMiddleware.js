function exceptionHandlingMiddleware(err, req, res, next) {
  try {
    console.error(' Failed', err.message);
    res.status(err.statusCode || 500).json({ error: { code: err.code || 'INTERNAL_ERROR', message: 'Unexpected error', correlationId: req.headers['x-correlation-id'] || null } });
  } catch (error) {
    console.error(' Failed', error.message);
    res.status(500).json({ error: { code: 'INTERNAL_ERROR', message: 'Unexpected error' } });
  }
}

module.exports = exceptionHandlingMiddleware;
