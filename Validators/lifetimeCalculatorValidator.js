const validateLifetimeCalculatorRequest = (body) => {
  if (!body || typeof body.dateOfBirth !== 'string' || !body.dateOfBirth.trim()) {
    return {
      valid: false,
      error: {
        message: 'dateOfBirth is required',
        code: 'VALIDATION_ERROR'
      }
    };
  }

  return { valid: true };
};

module.exports = { validateLifetimeCalculatorRequest };
