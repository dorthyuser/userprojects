function validateCreateOrderRequest(request) {
  if (!request || typeof request !== 'object') return 'Request body is required';
  if (!Number.isInteger(request.account_id) || request.account_id <= 0) return 'account_id must be greater than zero';
  if (!request.order_data || typeof request.order_data !== 'object' || Array.isArray(request.order_data)) return 'order_data is required';

  const { items, total, status } = request.order_data;
  if (!Array.isArray(items) || items.length === 0) return 'order_data.items must be a non-empty array';
  if (typeof total !== 'number' || Number.isNaN(total)) return 'order_data.total is required and must be numeric';
  if (typeof status !== 'string' || !status.trim()) return 'order_data.status is required';

  for (const item of items) {
    if (!item || typeof item !== 'object') return 'order_data.items entries must be objects';
    if (typeof item.product !== 'string' || !item.product.trim()) return 'order_data.items.product is required';
    if (!Number.isInteger(item.qty) || item.qty <= 0) return 'order_data.items.qty must be a positive integer';
    if (typeof item.price !== 'number' || Number.isNaN(item.price) || item.price < 0) return 'order_data.items.price must be a non-negative number';
  }

  return null;
}

function sanitizeAccountResponse(account) {
  return {
    id: account.id,
    name: account.name || '',
    email: maskEmail(account.email),
    address: maskAddress(account.address)
  };
}

function sanitizeOrderResponse(order) {
  return {
    id: order.id,
    account_id: order.account_id,
    order_data: order.order_data,
    created_at: order.created_at,
    ...(order.account ? { account: order.account } : {})
  };
}

function maskEmail(email) {
  if (!email || typeof email !== 'string') return '';
  const [local, domain] = email.split('@');
  if (!domain) return '***';
  return `${local.slice(0, 1)}***@${domain}`;
}

function maskAddress(address) {
  if (!address || typeof address !== 'string') return null;
  return address.length <= 3 ? '***' : `${address.slice(0, 3)}***`;
}

module.exports = { validateCreateOrderRequest, sanitizeOrderResponse, sanitizeAccountResponse };
