try {
  const calculateDob = async (req, res, next) => {
    try {
      const dob = req.query.dob || (req.body && req.body.dob);
      if (!dob) {
        const err = { message: 'dob is required', code: 'MISSING_DOB' };
        console.error(' Failed', err);
        return res.status(400).json({ error: err });
      }
      const dobDate = new Date(dob);
      if (isNaN(dobDate)) {
        const err = { message: 'Invalid dob format', code: 'INVALID_DOB' };
        console.error(' Failed', err);
        return res.status(400).json({ error: err });
      }
      const now = new Date();
      const diffMs = now - dobDate;
      const days = Math.floor(diffMs / (1000 * 60 * 60 * 24));
      const weeks = Math.floor(days / 7);
      const minutes = Math.floor(diffMs / (1000 * 60));
      const seconds = Math.floor(diffMs / 1000);
      const formatted = `${days} days, ${weeks} weeks, ${minutes} minutes, ${seconds} seconds`;
      const output = { days, weeks, minutes, seconds, formatted };
      return res.json(output);
    } catch (err) {
      console.error(' Failed', err);
      next(err);
    }
  };
  module.exports = { calculateDob };
  console.log(' Connected');
} catch (err) {
  console.error(' Failed', err);
  module.exports = {};
}
