const express = require('express');
const router = express.Router();
const familyController = require('../controllers/familyController');

try {
  console.log(' Connected');
  router.post('/', familyController.createFamily);
  router.post('/:family_id/members', familyController.inviteMember);
} catch (error) {
  console.log(' Failed', error && error.message ? error.message : error);
}

module.exports = router;
