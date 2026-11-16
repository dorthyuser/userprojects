const errorHandler = (err, req, res, next) => {
  try {
    console.error('Failed', err && err.message ? err.message : 'Unknown error');
    return res.status(500).json({
      error: {
        code: 'INTERNAL_SERVER_ERROR',
        message: 'Failed'
      }
    });
  } catch (error) {
    console.error('Failed', error.message);
    return res.status(500).json({
      error: {
        code: 'INTERNAL_SERVER_ERROR',
        message: 'Failed'
      }
    });
  }
};

module.exports = errorHandler;
