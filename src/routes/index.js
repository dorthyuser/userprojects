const express = require('express');
const { guessAge } = require('../controllers/ageController');

const router = express.Router();

router.get('/guess-age', guessAge);

module.exports = router;