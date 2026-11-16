function validateLifetimeInput(body) {
  try {
    if (!body || typeof body !== 'object') {
      return { valid: false, error: { message: 'Request body must be an object.' } };
    }

    if (typeof body.dateOfBirth !== 'string' || !body.dateOfBirth.trim()) {
      return { valid: false, error: { message: 'dateOfBirth is required and must be a string.' } };
    }

    const parsed = new Date(body.dateOfBirth);
    if (Number.isNaN(parsed.getTime())) {
      return { valid: false, error: { message: 'dateOfBirth must be a valid ISO 8601 date-time string.' } };
    }

    return { valid: true, value: { dateOfBirth: body.dateOfBirth } };
  } catch (error) {
    console.error(' Failed', error && error.message ? error.message : error);
    return { valid: false, error: { message: 'Validation failed.' } };
  }
}

module.exports = { validateLifetimeInput };
