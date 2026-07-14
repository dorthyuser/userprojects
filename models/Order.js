class Order {
  constructor({ id, account_id, order_data, created_at, account }) {
    this.id = id;
    this.account_id = account_id;
    this.order_data = order_data;
    this.created_at = created_at;
    this.account = account;
  }
}

module.exports = Order;
