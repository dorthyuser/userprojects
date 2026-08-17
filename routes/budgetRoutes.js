const express = require('express');
const router = express.Router();
const budgetController = require('../controllers/budgetController');

try {
  console.log(' Connected');
  router.post('/', budgetController.createBudget);
  router.get('/:id', budgetController.getBudgetById);
} catch (error) {
  console.log(' Failed', error && error.message ? error.message : error);
}

module.exports = router;
