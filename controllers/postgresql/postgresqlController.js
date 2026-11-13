const { differenceInYears, differenceInMonths, differenceInWeeks, differenceInDays, differenceInHours, differenceInMinutes, differenceInSeconds } = require('date-fns');

function safeErrorResponse(res, statusCode, code, message) {
  return res.status(statusCode).json({
    error: {
      code,
      message
    }
  });
}

exports.calculateLifetime = (req, res) => {
  try {
    const { dateOfBirth } = req.body || {};

    if (!dateOfBirth || typeof dateOfBirth !== 'string') {
      return safeErrorResponse(res, 400, 'VALIDATION_ERROR', 'Invalid request payload.');
    }

    const birthDate = new Date(dateOfBirth);
    if (Number.isNaN(birthDate.getTime())) {
      return safeErrorResponse(res, 400, 'VALIDATION_ERROR', 'Invalid date format.');
    }

    const currentDate = new Date();
    if (birthDate > currentDate) {
      return safeErrorResponse(res, 400, 'VALIDATION_ERROR', 'dateOfBirth cannot be in the future.');
    }

    const years = differenceInYears(currentDate, birthDate);
    const months = differenceInMonths(currentDate, birthDate);
    const weeks = differenceInWeeks(currentDate, birthDate);
    const days = differenceInDays(currentDate, birthDate);
    const hours = differenceInHours(currentDate, birthDate);
    const minutes = differenceInMinutes(currentDate, birthDate);
    const seconds = differenceInSeconds(currentDate, birthDate);

    return res.json({
      dateOfBirth: birthDate.toISOString(),
      currentDate: currentDate.toISOString(),
      age: {
        years,
        months,
        weeks,
        days,
        hours,
        minutes,
        seconds
      },
      lifetimeStats: {
        totalYearsLived: Number((days / 365.2425).toFixed(10)),
        totalMonthsLived: Number((days / 30.436875).toFixed(10)),
        totalWeeksLived: Number((days / 7).toFixed(10)),
        totalDaysLived: Number(days.toFixed(10)),
        totalHoursLived: Number((days * 24).toFixed(10)),
        totalMinutesLived: Number((days * 24 * 60).toFixed(10)),
        totalSecondsLived: Number((days * 24 * 60 * 60).toFixed(10))
      }
    });
  } catch (error) {
    console.error('postgresql Controller Failed', error && error.message ? error.message : 'Processing error');
    return safeErrorResponse(res, 500, 'INTERNAL_SERVER_ERROR', 'An internal error occurred.');
  }
};
