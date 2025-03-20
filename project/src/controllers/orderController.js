const OrderService = require('../services/orderService');

exports.getOrders = async (req, res) => {
    try {
        const orders = await OrderService.getOrders();
        res.status(200).json(orders);
    } catch (error) {
        console.error('Error fetching orders:', error);
        res.status(500).json({ error: 'Internal Server Error' });
    }
};

exports.createOrder = async (req, res) => {
    try {
        const orderData = req.body;
        const newOrder = await OrderService.createOrder(orderData);
        res.status(201).json(newOrder);
    } catch (error) {
        console.error('Error creating order:', error);
        res.status(400).json({ error: 'Bad Request' });
    }
};