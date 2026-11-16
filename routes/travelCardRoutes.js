const express = require('express');
const controller = require('../controllers/travelCardController');

const router = express.Router();

try {
  router.post('/', controller.createTravelCard);
  router.get('/:travelCardId', controller.getTravelCard);
  router.put('/:travelCardId', controller.updateTravelCard);
  router.delete('/:travelCardId', controller.deleteTravelCard);
  router.post('/:travelCardId/activate', controller.activateTravelCard);
  router.post('/:travelCardId/freeze', controller.freezeTravelCard);
  router.post('/:travelCardId/unfreeze', controller.unfreezeTravelCard);
  router.post('/:travelCardId/travel-notice', controller.updateTravelNotice);
  router.get('/:travelCardId/transactions', controller.getTravelCardTransactions);
  console.log(' Connected');
} catch (error) {
  console.error(' Failed', { message: error.message });
}

module.exports = router;
