try {
  const express = require('express');
  const salesforceRoutes = require('./salesforce/salesforceRoutes');

  const router = express.Router();

  router.use('/salesforce', salesforceRoutes);

  console.log('Connected routes/index.js');

  module.exports = router;
} catch (error) {
  console.log('Failed routes/index.js', error);
  module.exports = (req, res) => res.status(500).json({ errors: ['Routes failed to load'] });
}
