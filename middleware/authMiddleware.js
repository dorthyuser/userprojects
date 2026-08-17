module.exports = (req, res, next) => {
  try {
    console.log(' Connected');
    next();
  } catch (error) {
    console.log(' Failed', error && error.message ? error.message : error);
    res.status(401).json({ error: { code: 'AUTH_INVALID', message: 'Authentication failure', details: {} } });
  }
};
