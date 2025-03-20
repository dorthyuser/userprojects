const db = require('../config/db');

exports.getOrders = () => {
  return new Promise((resolve, reject) => {
    db.query('SELECT * FROM orders', (error, results) => {
      if (error) return reject(error);
      resolve(results);
    });
  });
};

exports.createOrder = (order) => {
  return new Promise((resolve, reject) => {
    const { customerId, orderDate, items } = order;
    const query = 'INSERT INTO orders (customerId, orderDate) VALUES (?, ?)';
    db.query(query, [customerId, orderDate], (error, result) => {
      if (error) return reject(error);
      const orderId = result.insertId;
      const itemPromises = items.map(item => {
        const itemQuery = 'INSERT INTO order_items (orderId, itemId, quantity, price) VALUES (?, ?, ?, ?)';
        return new Promise((resolve, reject) => {
          db.query(itemQuery, [orderId, item.itemId, item.quantity, item.price], (err) => {
            if (err) return reject(err);
            resolve();
          });
        });
      });
      Promise.all(itemPromises).then(() => {
        resolve({ orderId, customerId, orderDate, items });
      }).catch(reject);
    });
  });
};
