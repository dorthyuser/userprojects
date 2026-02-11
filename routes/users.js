const express = require('express');
const router = express.Router();
const usersController = require('../controllers/usersController');
try {
  router.get('/', usersController.getAge);
  console.log(' Connected');
  module.exports = router;
} catch (error) {
  console.log(' Failed', error);
  module.exports = router;
}
