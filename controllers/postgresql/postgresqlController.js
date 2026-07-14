const { PostgreSqlService } = require('../../services/postgresql/postgresqlService');

class PostgreSqlController {
  constructor() {
    this.service = new PostgreSqlService();
  }

  async getOrders(req, res) {
    try {
      const result = await this.service.getOrders();
      return res.status(200).json(result);
    } catch (error) {
      console.error(' Failed', error.message);
      return res.status(error.statusCode || 500).json({ error: { code: error.code || 'INTERNAL_ERROR', message: error.publicMessage || 'Unexpected error', correlationId: req.headers['x-correlation-id'] || null } });
    }
  }

  async createOrder(req, res) {
    try {
      const result = await this.service.createOrder(req.body);
      return res.status(201).json(result);
    } catch (error) {
      console.error(' Failed', error.message);
      return res.status(error.statusCode || 500).json({ error: { code: error.code || 'INTERNAL_ERROR', message: error.publicMessage || 'Unexpected error', correlationId: req.headers['x-correlation-id'] || null } });
    }
  }

  async deleteOrder(req, res) {
    try {
      const result = await this.service.deleteOrder(req.params.id);
      return res.status(200).json(result);
    } catch (error) {
      console.error(' Failed', error.message);
      return res.status(error.statusCode || 500).json({ error: { code: error.code || 'INTERNAL_ERROR', message: error.publicMessage || 'Unexpected error', correlationId: req.headers['x-correlation-id'] || null } });
    }
  }
}

module.exports = { PostgreSqlController };
