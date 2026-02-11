const express = require('express');
const router = express.Router();
try {
  const users = require('./users');
  router.use('/users', users);
  console.log(' Connected');
  module.exports = router;
} catch (error) {
  console.log(' Failed', error);
  module.exports = router;
}
