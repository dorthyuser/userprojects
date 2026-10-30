const express = require('express');
const { OrdersService } = require('../services/ordersService');

function createOrdersRouter({ db, logger }) {
  const router = express.Router();
  const service = new OrdersService({ db, logger });

  router.get('/', async (req, res, next) => {
    try {
      const data = await service.getOrders();
      res.status(200).json({ data });
    } catch (error) {
      next(error);
    }
  });

  router.post('/', async (req, res, next) => {
    try {
      const data = await service.createOrder(req.body, req.correlationId);
      res.status(201).json({ data });
    } catch (error) {
      next(error);
    }
  });

  router.delete('/:id', async (req, res, next) => {
    try {
      const result = await service.deleteOrder(req.params.id, req.correlationId);
      res.status(200).json(result);
    } catch (error) {
      next(error);
    }
  });

  return router;
}

module.exports = { createOrdersRouter };
