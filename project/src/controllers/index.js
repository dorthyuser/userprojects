const express = require('express');
const bodyParser = require('body-parser');
const orderController = require('./controllers/orderController');
const app = express();

app.use(bodyParser.json());

app.get('/orders', orderController.getOrders);
app.post('/orders', orderController.createOrder);

const PORT = process.env.PORT || 3000;
app.listen(PORT, () => {
    console.log(`Server is running on port ${PORT}`);
});