function requestLogger(req, res, next) {
  try {
    const requestId = req.headers['x-request-id'] || null;
    console.log(' Connected', { method: req.method, path: req.path, requestId });
    next();
  } catch (error) {
    console.error(' Failed', error && error.message ? error.message : error);
    next();
  }
}

module.exports = { requestLogger };
