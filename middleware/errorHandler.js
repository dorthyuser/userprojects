const createStructuredError = (statusCode, code, message) => ({ statusCode, code, message });

const notFoundHandler = (req, res) => {
  return res.status(404).json({ error: { code: 'NOT_FOUND', message: 'Resource not found' } });
};

const errorHandler = (err, req, res, next) => {
  try {
    const statusCode = err.statusCode || 500;
    const code = err.code || 'INTERNAL_ERROR';
    const message = err.message || 'Unexpected error';
    console.error(' Failed', { code, message });
    return res.status(statusCode).json({ error: { code, message } });
  } catch (error) {
    console.error(' Failed', { message: error.message });
    return res.status(500).json({ error: { code: 'INTERNAL_ERROR', message: 'Unexpected error' } });
  }
};

module.exports = { createStructuredError, errorHandler, notFoundHandler };
