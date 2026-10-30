const { validateCreateOrderRequest, sanitizeOrderResponse, sanitizeAccountResponse } = require('../validation/orders');
const { AppError } = require('../utils/errors');

class OrdersService {
  constructor({ db, logger }) {
    this.db = db;
    this.logger = logger;
  }

  async getOrders() {
    const sql = `
      SELECT o.id, o.account_id, o.order_data, o.created_at,
             a.id AS account_id, a.name, a.email, a.address
      FROM orders o
      INNER JOIN accounts a ON a.id = o.account_id
      ORDER BY o.id DESC;
    `;

    const result = await this.db.query(sql);
    return result.rows.map((row) => sanitizeOrderResponse({
      id: row.id,
      account_id: row.account_id,
      order_data: row.order_data,
      created_at: row.created_at,
      account: sanitizeAccountResponse({
        id: row.account_id,
        name: row.name,
        email: row.email,
        address: row.address
      })
    }));
  }

  async createOrder(payload, correlationId) {
    const validationError = validateCreateOrderRequest(payload);
    if (validationError) {
      throw new AppError('VALIDATION_ERROR', validationError, 400);
    }

    const created = await this.db.transaction(async (client) => {
      const sql = `
        INSERT INTO orders (account_id, order_data)
        VALUES ($1, $2::jsonb)
        RETURNING id, account_id, order_data, created_at;
      `;
      const result = await client.query(sql, [payload.account_id, JSON.stringify(payload.order_data)]);
      const row = result.rows[0];
      return sanitizeOrderResponse({
        id: row.id,
        account_id: row.account_id,
        order_data: row.order_data,
        created_at: row.created_at
      });
    });

    this.logger.info({ correlationId, orderId: created.id, accountId: created.account_id }, 'order_created');
    return created;
  }

  async deleteOrder(id, correlationId) {
    const orderId = Number(id);
    if (!Number.isInteger(orderId) || orderId <= 0) {
      throw new AppError('VALIDATION_ERROR', 'Invalid order id', 400);
    }

    const deleted = await this.db.transaction(async (client) => {
      const result = await client.query('DELETE FROM orders WHERE id = $1', [orderId]);
      return result.rowCount > 0;
    });

    if (!deleted) {
      throw new AppError('NOT_FOUND', 'Order not found', 404);
    }

    this.logger.info({ correlationId, orderId }, 'order_deleted');
    return { message: 'Order deleted successfully', id: orderId };
  }
}

module.exports = { OrdersService };
