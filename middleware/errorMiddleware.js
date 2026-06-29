const errorMiddleware = (err, req, res, next) => {
  try {
    console.error(' Failed', { message: err.message });
    return res.status(500).json({
      error: {
        code: 'INTERNAL_SERVER_ERROR',
        message: 'An unexpected error occurred',
        details: null
      }
    });
  } catch (error) {
    console.error(' Failed', { message: error.message });
    return res.status(500).json({
      error: {
        code: 'INTERNAL_SERVER_ERROR',
        message: 'An unexpected error occurred',
        details: null
      }
    });
  }
};

module.exports = errorMiddleware;
