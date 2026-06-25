const calculateLifetime = (req, res) => {
  try {
    const { dateOfBirth } = req.body;

    if (!dateOfBirth) {
      return res.status(400).json({
        error: {
          message: 'dateOfBirth is required',
          code: 'VALIDATION_ERROR'
        }
      });
    }

    const dob = new Date(dateOfBirth);
    const now = new Date();

    if (isNaN(dob.getTime())) {
      return res.status(400).json({
        error: {
          message: 'Invalid dateOfBirth format',
          code: 'INVALID_DATE'
        }
      });
    }

    if (dob > now) {
      return res.status(400).json({
        error: {
          message: 'dateOfBirth cannot be in the future',
          code: 'INVALID_RANGE'
        }
      });
    }

    let years = now.getFullYear() - dob.getFullYear();
    let months = now.getMonth() - dob.getMonth();
    let days = now.getDate() - dob.getDate();
    let hours = now.getHours() - dob.getHours();
    let minutes = now.getMinutes() - dob.getMinutes();
    let seconds = now.getSeconds() - dob.getSeconds();

    if (seconds < 0) { seconds += 60; minutes -= 1; }
    if (minutes < 0) { minutes += 60; hours -= 1; }
    if (hours < 0) { hours += 24; days -= 1; }
    if (days < 0) {
      const prevMonth = new Date(now.getFullYear(), now.getMonth(), 0).getDate();
      days += prevMonth;
      months -= 1;
    }
    if (months < 0) { months += 12; years -= 1; }

    const totalMilliseconds = now.getTime() - dob.getTime();
    const totalSecondsLived = Math.floor(totalMilliseconds / 1000);
    const totalMinutesLived = Math.floor(totalSecondsLived / 60);
    const totalHoursLived = Math.floor(totalMinutesLived / 60);
    const totalDaysLived = Math.floor(totalHoursLived / 24);
    const totalWeeksLived = Math.floor(totalDaysLived / 7);
    const totalMonthsLived = years * 12 + months;
    const totalYearsLived = years;

    return res.status(200).json({
      dateOfBirth,
      currentDate: now.toISOString(),
      age: {
        years,
        months,
        weeks: totalWeeksLived,
        days,
        hours,
        minutes,
        seconds
      },
      lifetimeStats: {
        totalYearsLived,
        totalMonthsLived,
        totalWeeksLived,
        totalDaysLived,
        totalHoursLived,
        totalMinutesLived,
        totalSecondsLived
      }
    });
  } catch (error) {
    console.error('Failed', error);
    return res.status(500).json({
      error: {
        message: 'Internal server error',
        code: 'SERVER_ERROR'
      }
    });
  }
};

module.exports = { calculateLifetime };
