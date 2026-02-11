try {
  const express = require('express');
  const router = express.Router();
  const controller = require('../controllers/userController');
  router.post('/', controller.calculateDob);
  module.exports = router;
  console.log(' Connected');
} catch (err) {
  console.error(' Failed', err);
  module.exports = null;
}
