function correlationMiddleware(req, res, next) {
  try {
    const correlationId = req.headers['x-correlation-id'] || `corr-${Date.now()}`;
    req.correlationId = correlationId;
    res.setHeader('x-correlation-id', correlationId);
    console.log(' Connected');
    next();
  } catch (error) {
    console.error(' Failed', error.message);
    next(error);
  }
}

module.exports = correlationMiddleware;
