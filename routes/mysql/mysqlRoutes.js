const express = require('express');
const router = express.Router();
const mysqlController = require('../../controllers/mysql/mysqlController');

router.get('/policy', mysqlController.getPasswordPolicy);
router.post('/generate', mysqlController.generatePassword);
router.post('/validate', mysqlController.validatePassword);

module.exports = router;
