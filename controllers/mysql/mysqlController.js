const mysqlConnection = require('../../connections/mysql');

const maskDateOfBirth = (value) => {
  try {
    if (!value || typeof value !== 'string') {
      return null;
    }
    return '****-**-**';
  } catch (error) {
    console.error('maskDateOfBirth Failed', {
      message: error.message,
      code: 'MASK_DOB_ERROR'
    });
    return null;
  }
};

const buildStructuredError = (code, message, requestId, details) => {
  try {
    return {
      error: {
        code,
        message,
        requestId,
        details: details || []
      }
    };
  } catch (error) {
    console.error('buildStructuredError Failed', {
      requestId: requestId || null,
      message: error.message,
      code: 'STRUCTURED_ERROR_BUILD_FAILURE'
    });
    return {
      error: {
        code: 'STRUCTURED_ERROR_BUILD_FAILURE',
        message: 'Internal server error',
        requestId: requestId || null
      }
    };
  }
};

const parseDateOnly = (value) => {
  try {
    if (!/^\d{4}-\d{2}-\d{2}$/.test(value)) {
      return null;
    }
    const date = new Date(`${value}T00:00:00Z`);
    if (Number.isNaN(date.getTime())) {
      return null;
    }
    return date;
  } catch (error) {
    console.error('parseDateOnly Failed', {
      message: error.message,
      code: 'PARSE_DATE_ERROR'
    });
    return null;
  }
};

const calculateAgeDifference = (dob, ref) => {
  try {
    let years = ref.getUTCFullYear() - dob.getUTCFullYear();
    let months = ref.getUTCMonth() - dob.getUTCMonth();
    let days = ref.getUTCDate() - dob.getUTCDate();

    if (days < 0) {
      const previousMonthDate = new Date(Date.UTC(ref.getUTCFullYear(), ref.getUTCMonth(), 0));
      days += previousMonthDate.getUTCDate();
      months -= 1;
    }

    if (months < 0) {
      months += 12;
      years -= 1;
    }

    return { years, months, days };
  } catch (error) {
    console.error('calculateAgeDifference Failed', {
      message: error.message,
      code: 'AGE_CALCULATION_ERROR'
    });
    throw error;
  }
};

module.exports = {
  health: async (req, res) => {
    try {
      return res.status(200).json({
        status: 'healthy',
        service: 'age-calculator',
        version: 'v1'
      });
    } catch (error) {
      console.error('health Failed', {
        requestId: req.requestId || null,
        message: error.message,
        code: 'HEALTH_ENDPOINT_ERROR'
      });
      return res.status(500).json(buildStructuredError('HEALTH_ENDPOINT_ERROR', 'Internal server error', req.requestId || null));
    }
  },

  calculateAge: async (req, res) => {
    try {
      const { dateOfBirth, referenceDate, consent } = req.body || {};

      const maskedLogPayload = {
        dateOfBirth: maskDateOfBirth(dateOfBirth),
        referenceDate: referenceDate || null,
        consent: typeof consent === 'boolean' ? consent : null
      };

      if (typeof consent !== 'boolean' || consent !== true) {
        console.error('calculateAge Failed', {
          requestId: req.requestId || null,
          code: 'CONSENT_REQUIRED',
          payload: maskedLogPayload
        });
        return res.status(400).json(buildStructuredError('CONSENT_REQUIRED', 'Explicit consent is required to process PII data', req.requestId || null, [{ field: 'consent', issue: 'must be true' }]));
      }

      if (!dateOfBirth || !referenceDate) {
        console.error('calculateAge Failed', {
          requestId: req.requestId || null,
          code: 'VALIDATION_ERROR',
          payload: maskedLogPayload
        });
        return res.status(400).json(buildStructuredError('VALIDATION_ERROR', 'Invalid request payload', req.requestId || null, [
          { field: 'dateOfBirth', issue: 'required' },
          { field: 'referenceDate', issue: 'required' }
        ]));
      }

      const dob = parseDateOnly(dateOfBirth);
      const ref = parseDateOnly(referenceDate);

      if (!dob || !ref) {
        console.error('calculateAge Failed', {
          requestId: req.requestId || null,
          code: 'INVALID_DATE_FORMAT',
          payload: maskedLogPayload
        });
        return res.status(400).json(buildStructuredError('INVALID_DATE_FORMAT', 'Dates must use YYYY-MM-DD format', req.requestId || null, [
          { field: 'dateOfBirth', issue: 'invalid format' },
          { field: 'referenceDate', issue: 'invalid format' }
        ]));
      }

      if (dob > ref) {
        console.error('calculateAge Failed', {
          requestId: req.requestId || null,
          code: 'INVALID_DATE_RANGE',
          payload: maskedLogPayload
        });
        return res.status(400).json(buildStructuredError('INVALID_DATE_RANGE', 'dateOfBirth cannot be later than referenceDate', req.requestId || null, [{ field: 'dateOfBirth', issue: 'must be on or before referenceDate' }]));
      }

      if (dob.getUTCFullYear() < 1900) {
        console.error('calculateAge Failed', {
          requestId: req.requestId || null,
          code: 'AML_RISK_RULE_TRIGGERED',
          payload: maskedLogPayload
        });
        return res.status(400).json(buildStructuredError('AML_RISK_RULE_TRIGGERED', 'Input failed policy validation', req.requestId || null, [{ field: 'dateOfBirth', issue: 'outside acceptable policy range' }]));
      }

      const age = calculateAgeDifference(dob, ref);

      try {
        if (mysqlConnection && typeof mysqlConnection.execute === 'function') {
          await mysqlConnection.execute('SELECT 1');
        }
        console.log('mysqlController Connected');
      } catch (dbError) {
        console.error('mysqlController Failed', {
          requestId: req.requestId || null,
          message: dbError.message,
          code: 'MYSQL_HEALTHCHECK_ERROR'
        });
      }

      return res.status(200).json({
        age,
        calculatedAt: `${referenceDate}T00:00:00Z`,
        dataClassification: {
          dateOfBirth: 'PII',
          referenceDate: 'Internal'
        },
        loggingPolicy: {
          maskFields: ['dateOfBirth'],
          excludeFields: []
        }
      });
    } catch (error) {
      console.error('calculateAge Failed', {
        requestId: req.requestId || null,
        message: error.message,
        code: 'CALCULATE_AGE_ERROR'
      });
      return res.status(500).json(buildStructuredError('CALCULATE_AGE_ERROR', 'Internal server error', req.requestId || null));
    }
  }
};
