try {
  exports.getAge = async (req, res, next) => {
    try {
      const dob = req.query.dob;
      if (!dob) {
        const err = { status: 400, message: 'dob query parameter is required' };
        console.log(' Failed', err);
        return res.status(err.status).json({ error: { message: err.message } });
      }
      const dobDate = new Date(dob);
      if (isNaN(dobDate.getTime())) {
        const err = { status: 400, message: 'dob is not a valid date' };
        console.log(' Failed', err);
        return res.status(err.status).json({ error: { message: err.message } });
      }
      const now = new Date();
      const diffMs = now - dobDate;
      if (diffMs < 0) {
        const err = { status: 400, message: 'dob is in the future' };
        console.log(' Failed', err);
        return res.status(err.status).json({ error: { message: err.message } });
      }
      const seconds = Math.floor(diffMs / 1000);
      const minutes = Math.floor(diffMs / (1000 * 60));
      const days = Math.floor(diffMs / (1000 * 60 * 60 * 24));
      const weeks = Math.floor(days / 7);
      const formatted = `${days} days, ${weeks} weeks, ${minutes} minutes, ${seconds} seconds`;
      return res.json({ days, weeks, minutes, seconds, formatted });
    } catch (error) {
      console.log(' Failed', error);
      return res.status(500).json({ error: { message: 'Internal Server Error' } });
    }
  };
  console.log(' Connected');
} catch (error) {
  console.log(' Failed', error);
}
