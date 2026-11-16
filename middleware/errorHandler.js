module.exports = (err, req, res, next) => {
  try {
    console.error(' Failed', { message: err.message });
    return res.status(err.statusCode || 500).json({
      error: {
        code: err.code || 'INTERNAL_ERROR',
        message: err.publicMessage || 'Unexpected error'
      }
    });
  } catch (error) {
    console.error(' Failed', { message: error.message });
    return res.status(500).json({ error: { code: 'INTERNAL_ERROR', message: 'Unexpected error' } });
  }
};
