const { calculateAgeMetrics } = require('../models/ageMetricsModel');

const createAgeMetrics = (req, res) => {
  try {
    const { dateOfBirth } = req.body || {};

    if (!dateOfBirth || typeof dateOfBirth !== 'string') {
      return res.status(400).json({
        error: {
          code: 'VALIDATION_ERROR',
          message: 'Invalid request payload',
          details: [{ field: 'dateOfBirth', issue: 'dateOfBirth is required in YYYY-MM-DD format' }]
        }
      });
    }

    const result = calculateAgeMetrics(dateOfBirth);
    return res.status(200).json(result);
  } catch (error) {
    console.error('Failed', error.message);
    return res.status(400).json({
      error: {
        code: 'BAD_REQUEST',
        message: 'Failed',
        details: [{ issue: 'Unable to process dateOfBirth' }]
      }
    });
  }
};

module.exports = { createAgeMetrics };
