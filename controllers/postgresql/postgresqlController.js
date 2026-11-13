const { postgresqlConnection } = require('../../connections/postgresql');

function parseDateTime(input) {
  const date = new Date(input);
  return Number.isNaN(date.getTime()) ? null : date;
}

function diffParts(fromDate, toDate) {
  let years = toDate.getUTCFullYear() - fromDate.getUTCFullYear();
  let months = toDate.getUTCMonth() - fromDate.getUTCMonth();
  let days = toDate.getUTCDate() - fromDate.getUTCDate();
  let hours = toDate.getUTCHours() - fromDate.getUTCHours();
  let minutes = toDate.getUTCMinutes() - fromDate.getUTCMinutes();
  let seconds = toDate.getUTCSeconds() - fromDate.getUTCSeconds();

  if (seconds < 0) { seconds += 60; minutes -= 1; }
  if (minutes < 0) { minutes += 60; hours -= 1; }
  if (hours < 0) { hours += 24; days -= 1; }
  if (days < 0) {
    const prevMonth = new Date(Date.UTC(toDate.getUTCFullYear(), toDate.getUTCMonth(), 0));
    days += prevMonth.getUTCDate();
    months -= 1;
  }
  if (months < 0) { months += 12; years -= 1; }

  const totalMs = toDate.getTime() - fromDate.getTime();
  const totalSeconds = Math.floor(totalMs / 1000);
  const totalMinutes = totalSeconds / 60;
  const totalHours = totalMinutes / 60;
  const totalDays = totalHours / 24;
  const totalWeeks = totalDays / 7;
  const totalMonths = years * 12 + months + days / 30.4375;
  const totalYears = totalMonths / 12;

  return {
    age: { years, months, weeks: Math.floor(totalWeeks), days, hours, minutes, seconds },
    lifetimeStats: {
      totalYearsLived: Number(totalYears.toFixed(6)),
      totalMonthsLived: Number(totalMonths.toFixed(6)),
      totalWeeksLived: Number(totalWeeks.toFixed(6)),
      totalDaysLived: Number(totalDays.toFixed(6)),
      totalHoursLived: Number(totalHours.toFixed(6)),
      totalMinutesLived: Number(totalMinutes.toFixed(6)),
      totalSecondsLived: Number(totalSeconds.toFixed(0))
    }
  };
}

const calculateLifetime = async (req, res) => {
  try {
    const { dateOfBirth } = req.body || {};
    const birthDate = parseDateTime(dateOfBirth);

    if (!dateOfBirth || !birthDate) {
      return res.status(400).json({
        error: {
          code: 'VALIDATION_ERROR',
          message: 'Invalid or missing dateOfBirth'
        }
      });
    }

    const currentDate = new Date();
    if (birthDate.getTime() > currentDate.getTime()) {
      return res.status(400).json({
        error: {
          code: 'VALIDATION_ERROR',
          message: 'dateOfBirth cannot be in the future'
        }
      });
    }

    const result = diffParts(birthDate, currentDate);

    if (postgresqlConnection) {
      console.log('PostgreSql Connected');
    } else {
      console.log('PostgreSql Failed');
    }

    return res.status(200).json({
      dateOfBirth: birthDate.toISOString(),
      currentDate: currentDate.toISOString(),
      age: result.age,
      lifetimeStats: result.lifetimeStats
    });
  } catch (error) {
    console.log('PostgreSql Failed', error && error.message ? error.message : 'Controller error');
    return res.status(500).json({
      error: {
        code: 'INTERNAL_SERVER_ERROR',
        message: 'An unexpected error occurred'
      }
    });
  }
};

module.exports = { calculateLifetime };
