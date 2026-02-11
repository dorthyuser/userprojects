try {
  module.exports = (err, req, res, next) => {
    console.error(' Failed', err);
    res.status(err.status || 500).json({
      error: {
        message: err.message || 'Internal Server Error',
        code: err.code || 'SERVER_ERROR',
        details: err.details || null
      }
    });
  };
  console.log(' Connected');
} catch (err) {
  console.error(' Failed', err);
  module.exports = (err, req, res, next) => {
    res.status(500).json({ error: { message: 'Initialization failed' } });
  };
}
