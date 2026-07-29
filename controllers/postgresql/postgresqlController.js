const crypto = require('crypto');
const postgresqlConnection = require('../../connections/postgresql');

const maskSensitive = (value) => {
  if (!value) return value;
  return '[REDACTED]';
};

const safeError = (res, statusCode, code, message, details = {}) => {
  return res.status(statusCode).json({ error: { code, message, details } });
};

const validateUuid = (value) => /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(String(value || ''));

const hashToken = (token) => crypto.createHash('sha256').update(String(token || '')).digest('hex');

exports.registerUser = async (req, res) => {
  try {
    const { name, email, password, role } = req.body || {};
    if (!name || !email || !password || !role) return safeError(res, 400, 'VALIDATION_ERROR', 'Bad input', { name: 'required', email: 'required', password: 'required', role: 'required' });
    if (!['customer', 'restaurant', 'delivery'].includes(role)) return safeError(res, 400, 'VALIDATION_ERROR', 'Bad input', { role: 'invalid role' });
    const client = await postgresqlConnection.connect();
    try {
      const existing = await client.query('SELECT id FROM users WHERE email = $1 LIMIT 1', [email]);
      if (existing.rows.length) return safeError(res, 409, 'DUPLICATE_EMAIL', 'Email already exists');
      const result = await client.query('INSERT INTO users (name, email, password_hash, role) VALUES ($1, $2, $3, $4) RETURNING id, name, email, role, created_at', [name, email, hashToken(password), role]);
      return res.status(201).json(result.rows[0]);
    } finally {
      client.release();
    }
  } catch (error) {
    console.error('registerUser Failed', { error: error.message, email: maskSensitive(req.body && req.body.email) });
    return safeError(res, 500, 'INTERNAL_SERVER_ERROR', 'Unexpected server error');
  }
};

exports.loginUser = async (req, res) => {
  try {
    const { email, password } = req.body || {};
    if (!email || !password) return safeError(res, 400, 'VALIDATION_ERROR', 'Bad input', { email: 'required', password: 'required' });
    return res.status(200).json({ access_token: 'jwt', refresh_token: 'jwt_refresh', expires_in: 3600 });
  } catch (error) {
    console.error('loginUser Failed', { error: error.message, email: maskSensitive(req.body && req.body.email) });
    return safeError(res, 500, 'INTERNAL_SERVER_ERROR', 'Unexpected server error');
  }
};

exports.refreshToken = async (req, res) => {
  try {
    const { refresh_token } = req.body || {};
    if (!refresh_token) return safeError(res, 400, 'VALIDATION_ERROR', 'Bad input', { refresh_token: 'required' });
    return res.status(200).json({ access_token: 'jwt', expires_in: 3600 });
  } catch (error) {
    console.error('refreshToken Failed', { error: error.message });
    return safeError(res, 500, 'INTERNAL_SERVER_ERROR', 'Unexpected server error');
  }
};

exports.listRestaurants = async (req, res) => {
  try {
    return res.status(200).json({ page: 1, per_page: 20, total: 0, items: [] });
  } catch (error) {
    console.error('listRestaurants Failed', { error: error.message });
    return safeError(res, 500, 'INTERNAL_SERVER_ERROR', 'Unexpected server error');
  }
};

exports.getRestaurantMenu = async (req, res) => {
  try {
    if (!validateUuid(req.params.id)) return safeError(res, 400, 'VALIDATION_ERROR', 'Bad input', { id: 'invalid uuid' });
    return res.status(200).json({ restaurant_id: req.params.id, items: [] });
  } catch (error) {
    console.error('getRestaurantMenu Failed', { error: error.message, restaurant_id: req.params.id });
    return safeError(res, 500, 'INTERNAL_SERVER_ERROR', 'Unexpected server error');
  }
};

exports.createOrder = async (req, res) => {
  try {
    return res.status(201).json({ order_id: crypto.randomUUID(), status: 'pending', total_cents: 0, created_at: new Date().toISOString() });
  } catch (error) {
    console.error('createOrder Failed', { error: error.message });
    return safeError(res, 500, 'INTERNAL_SERVER_ERROR', 'Unexpected server error');
  }
};

exports.getOrderById = async (req, res) => {
  try {
    if (!validateUuid(req.params.id)) return safeError(res, 400, 'VALIDATION_ERROR', 'Bad input', { id: 'invalid uuid' });
    return res.status(200).json({ order_id: req.params.id, status: 'pending', items: [], delivery: { delivery_id: crypto.randomUUID(), eta_mins: 0 } });
  } catch (error) {
    console.error('getOrderById Failed', { error: error.message, order_id: req.params.id });
    return safeError(res, 500, 'INTERNAL_SERVER_ERROR', 'Unexpected server error');
  }
};

exports.updateDeliveryLocation = async (req, res) => {
  try {
    return res.status(200).json({ status: 'ok', recorded_at: new Date().toISOString() });
  } catch (error) {
    console.error('updateDeliveryLocation Failed', { error: error.message, delivery_id: req.params.delivery_id });
    return safeError(res, 500, 'INTERNAL_SERVER_ERROR', 'Unexpected server error');
  }
};

exports.getDeliveryLocations = async (req, res) => {
  try {
    if (!validateUuid(req.params.delivery_id)) return safeError(res, 400, 'VALIDATION_ERROR', 'Bad input', { delivery_id: 'invalid uuid' });
    return res.status(200).json({ delivery_id: req.params.delivery_id, points: [] });
  } catch (error) {
    console.error('getDeliveryLocations Failed', { error: error.message, delivery_id: req.params.delivery_id });
    return safeError(res, 500, 'INTERNAL_SERVER_ERROR', 'Unexpected server error');
  }
};
