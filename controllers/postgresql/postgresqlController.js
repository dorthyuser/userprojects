const postgresqlConnection = require('../../connections/postgresql');

function calculateLifetime(req, res) {
  try {
    const { dateOfBirth } = req.body || {};

    if (!dateOfBirth || typeof dateOfBirth !== 'string') {
      return res.status(400).json({
        success: false,
        error: {
          code: 'VALIDATION_ERROR',
          message: 'dateOfBirth is required and must be a valid date-time string.'
        }
      });
    }

    const dob = new Date(dateOfBirth);
    if (Number.isNaN(dob.getTime())) {
      return res.status(400).json({
        success: false,
        error: {
          code: 'VALIDATION_ERROR',
          message: 'dateOfBirth must be a valid ISO date-time string.'
        }
      });
    }

    const now = new Date();
    if (dob > now) {
      return res.status(400).json({
        success: false,
        error: {
          code: 'VALIDATION_ERROR',
          message: 'dateOfBirth cannot be in the future.'
        }
      });
    }

    const diffMs = now.getTime() - dob.getTime();
    const totalSeconds = Math.floor(diffMs / 1000);
    const totalMinutes = Math.floor(totalSeconds / 60);
    const totalHours = Math.floor(totalMinutes / 60);
    const totalDays = Math.floor(totalHours / 24);
    const totalWeeks = Math.floor(totalDays / 7);

    let years = now.getUTCFullYear() - dob.getUTCFullYear();
    let months = now.getUTCMonth() - dob.getUTCMonth();
    let days = now.getUTCDate() - dob.getUTCDate();
    let hours = now.getUTCHours() - dob.getUTCHours();
    let minutes = now.getUTCMinutes() - dob.getUTCMinutes();
    let seconds = now.getUTCSeconds() - dob.getUTCSeconds();

    if (seconds < 0) { seconds += 60; minutes -= 1; }
    if (minutes < 0) { minutes += 60; hours -= 1; }
    if (hours < 0) { hours += 24; days -= 1; }
    if (days < 0) { months -= 1; const prevMonth = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), 0)); days += prevMonth.getUTCDate(); }
    if (months < 0) { months += 12; years -= 1; }

    const response = {
      dateOfBirth: dob.toISOString(),
      currentDate: now.toISOString(),
      age: {
        years: Math.max(0, years),
        months: Math.max(0, months),
        weeks: Math.max(0, totalWeeks),
        days: Math.max(0, totalDays),
        hours: Math.max(0, totalHours),
        minutes: Math.max(0, totalMinutes),
        seconds: Math.max(0, totalSeconds)
      },
      lifetimeStats: {
        totalYearsLived: Math.max(0, diffMs / (365.2425 * 24 * 60 * 60 * 1000)),
        totalMonthsLived: Math.max(0, diffMs / (30.436875 * 24 * 60 * 60 * 1000)),
        totalWeeksLived: Math.max(0, diffMs / (7 * 24 * 60 * 60 * 1000)),
        totalDaysLived: Math.max(0, diffMs / (24 * 60 * 60 * 1000)),
        totalHoursLived: Math.max(0, diffMs / (60 * 60 * 1000)),
        totalMinutesLived: Math.max(0, diffMs / (60 * 1000)),
        totalSecondsLived: Math.max(0, diffMs / 1000)
      }
    };

    return res.status(200).json(response);
  } catch (error) {
    console.error('PostgreSql Controller Failed', { message: error.message });
    return res.status(500).json({
      success: false,
      error: {
        code: 'INTERNAL_SERVER_ERROR',
        message: 'An unexpected error occurred.'
      }
    });
  }
}

module.exports = { calculateLifetime };
