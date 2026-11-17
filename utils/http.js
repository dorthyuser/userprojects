const { ValidationError, NotFoundError, ConflictError } = require('./errors');

function parseBody(body) {
  if (!body) return {};
  try { return JSON.parse(body); } catch { throw new ValidationError('Invalid JSON body'); }
}

function parseQuery(query) { return query || {}; }

function jsonResponse(statusCode, payload) {
  return { statusCode, headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload) };
}

function handleError(error) {
  const code = error && error.code ? error.code : 'INTERNAL_ERROR';
  const statusCode = error instanceof ValidationError ? 400 : error instanceof NotFoundError ? 404 : error instanceof ConflictError ? 409 : 500;
  console.log(JSON.stringify({ level: 'error', code, message: error.message }));
  return jsonResponse(statusCode, { error: { code, message: statusCode === 500 ? 'Internal server error' : error.message } });
}

module.exports = { parseBody, parseQuery, jsonResponse, handleError };