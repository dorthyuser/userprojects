const pino = require('pino');

function createLogger() {
  return pino({
    level: process.env.LOG_LEVEL || 'info',
    redact: {
      paths: [
        'req.headers.authorization',
        'req.headers.cookie',
        'password',
        'POSTGRESQL_PASSWORD',
        'POSTGRESQL_CONNECTION_STRING'
      ],
      censor: '[REDACTED]'
    },
    serializers: {
      err(error) {
        return {
          type: error.type,
          message: error.message,
          name: error.name,
          stack: process.env.NODE_ENV === 'production' ? undefined : error.stack
        };
      }
    }
  });
}

module.exports = { createLogger };
