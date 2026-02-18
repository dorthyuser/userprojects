const jwt = require("jsonwebtoken");

function validateBearerToken(authHeader, clientIdHeader, logger) {
  if (!authHeader || typeof authHeader !== "string") {
    const err = new Error("Missing Authorization header");
    err.status = 401;
    throw err;
  }
  const parts = authHeader.split(" ");
  if (parts.length !== 2 || parts[0] !== "Bearer") {
    const err = new Error("Invalid Authorization header format");
    err.status = 401;
    throw err;
  }
  const token = parts[1];
  const secret = process.env.IAM_JWT_SECRET;
  if (!secret) {
    const err = new Error("Server not configured with IAM_JWT_SECRET");
    err.status = 500;
    throw err;
  }
  try {
    const payload = jwt.verify(token, secret);
    if (!payload || !payload.client_id) {
      const err = new Error("Token missing client_id claim");
      err.status = 401;
      throw err;
    }
    if (clientIdHeader && payload.client_id !== clientIdHeader) {
      const err = new Error(
        "client_id in token does not match client_id header"
      );
      err.status = 401;
      throw err;
    }
    return payload;
  } catch (e) {
    logger && logger("Token validation error", e.message || e);
    const err = new Error("Invalid token");
    err.status = 401;
    throw err;
  }
}

module.exports = {
  validateBearerToken,
};
