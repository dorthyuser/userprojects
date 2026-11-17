class AppError extends Error { constructor(code, message) { super(message); this.code = code; } }
class ValidationError extends AppError { constructor(message) { super('VALIDATION_ERROR', message); } }
class NotFoundError extends AppError { constructor(message) { super('NOT_FOUND', message); } }
class ConflictError extends AppError { constructor(message) { super('CONFLICT', message); } }
module.exports = { AppError, ValidationError, NotFoundError, ConflictError };