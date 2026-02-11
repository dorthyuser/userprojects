try {
  module.exports = (err, req, res, next) => {
    try {
      console.log(' Failed', err);
      const status = err.status || 500;
      const message = err.message || 'Internal Server Error';
      const details = err.details || null;
      return res.status(status).json({ error: { message, details } });
    } catch (error) {
      console.log(' Failed', error);
      return res.status(500).json({ error: { message: 'Internal Server Error' } });
    }
  };
  console.log(' Connected');
} catch (error) {
  console.log(' Failed', error);
}
