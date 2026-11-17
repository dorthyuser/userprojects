function jsonResponse(status, body) {
  return {
    status,
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(body),
  };
}

function errorResponse(status, code, message, details) {
  const payload = { error: { code, message } };
  if (details && details.length) payload.error.details = details;
  return jsonResponse(status, payload);
}

module.exports = { jsonResponse, errorResponse };
