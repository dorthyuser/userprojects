const db = require('../db');

exports.getOrders = async () => {
    const [results] = await db.query('SELECT * FROM orders');
    return results;
};

exports.createOrder = async (orderData) => {
    const { customerId, orderDate, items } = orderData;
    const result = await db.query('INSERT INTO orders (customerId, orderDate) VALUES (?, ?)', [customerId, orderDate]);
    const orderId = result.insertId;
    await Promise.all(items.map(item => {
        return db.query('INSERT INTO order_items (orderId, itemId, quantity, price) VALUES (?, ?, ?, ?)', [orderId, item.itemId, item.quantity, item.price]);
    }));
    return { orderId, customerId, orderDate, items };
};