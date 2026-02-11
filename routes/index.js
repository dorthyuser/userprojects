try {
  const express = require('express');
  const router = express.Router();
  const dobRoutes = require('./dob');
  router.use('/', dobRoutes);
  module.exports = router;
  console.log(' Connected');
} catch (err) {
  console.error(' Failed', err);
  module.exports = null;
}
