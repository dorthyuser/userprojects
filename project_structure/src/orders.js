const express = require('express');
const mysql = require('mysql');
const bodyParser = require('body-parser');
const app = express();

app.use(bodyParser.json());

const db = mysql.createConnection({
    host: process.env.DB_HOST,
    user: process.env.DB_USER,
    password: process.env.DB_PASSWORD,
    database: process.env.DB_NAME
});

db.connect(err => {
    if (err) {
        console.error('Error connecting to the database:', err);
        return;
    }
    console.log('Connected to the database');
});

app.get('/orders', (req, res) => {
    db.query('SELECT * FROM orders', (err, results) => {
        if (err) {
            console.error('Error fetching orders:', err);
            return res.status(500).json({ error: 'Internal Server Error' });
        }
        res.json(results);
    });
});

app.post('/orders', (req, res) => {
    const { customerId, orderDate, items } = req.body;
    const order = { customerId, orderDate };

    db.query('INSERT INTO orders SET ?', order, (err, result) => {
        if (err) {
            console.error('Error creating order:', err);
            return res.status(500).json({ error: 'Internal Server Error' });
        }
        const orderId = result.insertId;
        const itemQueries = items.map(item => db.query('INSERT INTO order_items SET ?', { ...item, orderId }));

        Promise.all(itemQueries)
            .then(() => {
                res.status(201).json({ message: 'Order created', orderId });
            })
            .catch(err => {
                console.error('Error adding order items:', err);
                res.status(500).json({ error: 'Internal Server Error' });
            });
    });
});

const PORT = process.env.PORT || 3000;
app.listen(PORT, () => {
    console.log(`Server running on port ${PORT}`);
});