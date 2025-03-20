const express = require('express');
const orderController = require('../controllers/orderController');

const router = express.Router();

router.get('/orders', orderController.getOrders);
router.post('/orders', orderController.createOrder);

module.exports = router;
