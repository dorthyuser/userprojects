const db = require('./db');
const winston = require('winston');

const getOrders = async (req, res) => {
    try {
        const orders = await db.query('SELECT * FROM orders');
        res.json({ orders });
    } catch (error) {
        winston.error(error);
        res.status(500).json({ error: 'Internal Server Error' });
    }
};

const createOrder = async (req, res) => {
    const { customerId, orderDate, items } = req.body;
    try {
        const newOrder = await db.query('INSERT INTO orders (customerId, orderDate, items) VALUES (?, ?, ?)', [customerId, orderDate, JSON.stringify(items)]);
        res.status(201).json({ orderId: newOrder.insertId });
    } catch (error) {
        winston.error(error);
        res.status(400).json({ error: 'Bad Request', details: error.message });
    }
};

module.exports = { getOrders, createOrder };