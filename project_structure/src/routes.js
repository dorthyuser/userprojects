const express = require('express');
const { getOrders, createOrder } = require('./orders');

const router = express.Router();

router.get('/orders', getOrders);
router.post('/orders', createOrder);

module.exports = router;