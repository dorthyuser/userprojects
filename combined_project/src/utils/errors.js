class AppError extends Error {
  constructor(code, publicMessage, statusCode = 500) {
    super(publicMessage);
    this.name = 'AppError';
    this.code = code;
    this.publicMessage = publicMessage;
    this.statusCode = statusCode;
  }
}

module.exports = { AppError };
