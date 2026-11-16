const { validateLifetimeInput } = require('../models/lifetimeCalculatorModel');

function calculateLifetime(req, res) {
  try {
    const validation = validateLifetimeInput(req.body || {});
    if (!validation.valid) {
      console.error(' Failed', { message: validation.error.message, requestId: req.headers['x-request-id'] || null });
      return res.status(400).json({ error: { code: 'VALIDATION_ERROR', message: 'Invalid request payload.' } });
    }

    const dateOfBirth = new Date(validation.value.dateOfBirth);
    const currentDate = new Date();

    if (Number.isNaN(dateOfBirth.getTime())) {
      console.error(' Failed', { message: 'Invalid dateOfBirth format', requestId: req.headers['x-request-id'] || null });
      return res.status(400).json({ error: { code: 'VALIDATION_ERROR', message: 'Invalid request payload.' } });
    }

    if (dateOfBirth.getTime() > currentDate.getTime()) {
      console.error(' Failed', { message: 'dateOfBirth is in the future', requestId: req.headers['x-request-id'] || null });
      return res.status(400).json({ error: { code: 'VALIDATION_ERROR', message: 'Invalid request payload.' } });
    }

    const diffMs = currentDate.getTime() - dateOfBirth.getTime();
    const totalSeconds = Math.floor(diffMs / 1000);
    const totalMinutes = Math.floor(totalSeconds / 60);
    const totalHours = Math.floor(totalMinutes / 60);
    const totalDays = Math.floor(totalHours / 24);
    const totalWeeks = Math.floor(totalDays / 7);

    let years = currentDate.getUTCFullYear() - dateOfBirth.getUTCFullYear();
    let months = currentDate.getUTCMonth() - dateOfBirth.getUTCMonth();
    let days = currentDate.getUTCDate() - dateOfBirth.getUTCDate();
    let hours = currentDate.getUTCHours() - dateOfBirth.getUTCHours();
    let minutes = currentDate.getUTCMinutes() - dateOfBirth.getUTCMinutes();
    let seconds = currentDate.getUTCSeconds() - dateOfBirth.getUTCSeconds();

    if (seconds < 0) { seconds += 60; minutes -= 1; }
    if (minutes < 0) { minutes += 60; hours -= 1; }
    if (hours < 0) { hours += 24; days -= 1; }
    if (days < 0) { months -= 1; const prevMonth = new Date(Date.UTC(currentDate.getUTCFullYear(), currentDate.getUTCMonth(), 0)); days += prevMonth.getUTCDate(); }
    if (months < 0) { months += 12; years -= 1; }

    const response = {
      dateOfBirth: validation.value.dateOfBirth,
      currentDate: currentDate.toISOString(),
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
        totalYearsLived: Math.max(0, diffMs / (1000 * 60 * 60 * 24 * 365.2425)),
        totalMonthsLived: Math.max(0, diffMs / (1000 * 60 * 60 * 24 * 30.436875)),
        totalWeeksLived: Math.max(0, diffMs / (1000 * 60 * 60 * 24 * 7)),
        totalDaysLived: Math.max(0, diffMs / (1000 * 60 * 60 * 24)),
        totalHoursLived: Math.max(0, diffMs / (1000 * 60 * 60)),
        totalMinutesLived: Math.max(0, diffMs / (1000 * 60)),
        totalSecondsLived: Math.max(0, diffMs / 1000)
      }
    };

    return res.status(200).json(response);
  } catch (error) {
    console.error(' Failed', { message: error && error.message ? error.message : 'Controller error', requestId: req.headers['x-request-id'] || null });
    return res.status(500).json({ error: { code: 'INTERNAL_SERVER_ERROR', message: 'An internal error occurred.' } });
  }
}

module.exports = { calculateLifetime };
