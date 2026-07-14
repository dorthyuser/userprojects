const express = require('express');
const { PostgreSqlController } = require('../../controllers/postgresql/postgresqlController');

const router = express.Router();
const controller = new PostgreSqlController();

try {
  router.get('/', controller.getOrders.bind(controller));
  router.post('/', controller.createOrder.bind(controller));
  router.delete('/:id', controller.deleteOrder.bind(controller));
  console.log(' Connected');
} catch (error) {
  console.error(' Failed', error.message);
}

module.exports = router;
