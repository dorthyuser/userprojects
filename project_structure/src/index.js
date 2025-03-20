const express = require('express');
const bodyParser = require('body-parser');
const { getOrders, createOrder } = require('./orderController');

const app = express();
const PORT = process.env.PORT || 3000;

app.use(bodyParser.json());

app.get('/orders', getOrders);
app.post('/orders', createOrder);

app.listen(PORT, () => {
    console.log(`Server is running on port ${PORT}`);
});