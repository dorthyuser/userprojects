const express = require('express');
const travelCardRoutes = require('./travelCardRoutes');

const router = express.Router();

try {
  router.use('/travel-cards', travelCardRoutes);
  console.log(' Connected');
} catch (error) {
  console.error(' Failed', { message: error.message });
}

module.exports = router;
