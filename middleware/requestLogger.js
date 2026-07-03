const requestLogger = (req, res, next) => {
  try {
    const safeBody = req.body && typeof req.body === 'object' ? { ...req.body } : {};
    if (safeBody.amount) safeBody.amount = '[MASKED]';
    console.log(' Connected', { method: req.method, path: req.path });
    next();
  } catch (error) {
    console.error(' Failed', { message: error.message });
    next();
  }
};

module.exports = { requestLogger };
