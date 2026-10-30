const lifetimeCalculationSchema = {
  dateOfBirth: {
    type: 'string',
    required: true,
    sensitivity: 'PII',
    constraints: {
      format: 'date-time'
    }
  },
  currentDate: {
    type: 'string',
    required: true,
    sensitivity: 'system'
  },
  ageYears: {
    type: 'number',
    required: true,
    sensitivity: 'derived'
  },
  ageMonths: {
    type: 'number',
    required: true,
    sensitivity: 'derived'
  },
  ageWeeks: {
    type: 'number',
    required: true,
    sensitivity: 'derived'
  },
  ageDays: {
    type: 'number',
    required: true,
    sensitivity: 'derived'
  },
  ageHours: {
    type: 'number',
    required: true,
    sensitivity: 'derived'
  },
  ageMinutes: {
    type: 'number',
    required: true,
    sensitivity: 'derived'
  },
  ageSeconds: {
    type: 'number',
    required: true,
    sensitivity: 'derived'
  },
  totalYearsLived: {
    type: 'number',
    required: true,
    sensitivity: 'derived'
  },
  totalMonthsLived: {
    type: 'number',
    required: true,
    sensitivity: 'derived'
  },
  totalWeeksLived: {
    type: 'number',
    required: true,
    sensitivity: 'derived'
  },
  totalDaysLived: {
    type: 'number',
    required: true,
    sensitivity: 'derived'
  },
  totalHoursLived: {
    type: 'number',
    required: true,
    sensitivity: 'derived'
  },
  totalMinutesLived: {
    type: 'number',
    required: true,
    sensitivity: 'derived'
  },
  totalSecondsLived: {
    type: 'number',
    required: true,
    sensitivity: 'derived'
  }
};

module.exports = { lifetimeCalculationSchema };
