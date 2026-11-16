const calculateAgeMetrics = (dateOfBirth) => {
  try {
    const dob = new Date(dateOfBirth);
    if (Number.isNaN(dob.getTime())) {
      throw new Error('Invalid dateOfBirth');
    }

    const now = new Date();
    if (dob > now) {
      throw new Error('dateOfBirth cannot be in the future');
    }

    const diffMs = now.getTime() - dob.getTime();
    const ageInSeconds = Math.floor(diffMs / 1000);
    const ageInMinutes = Math.floor(ageInSeconds / 60);
    const ageInDays = Math.floor(ageInMinutes / 1440);
    const ageInWeeks = Math.floor(ageInDays / 7);

    return {
      ageInDays,
      ageInWeeks,
      ageInMinutes,
      ageInSeconds
    };
  } catch (error) {
    console.error('Failed', error.message);
    throw error;
  }
};

module.exports = { calculateAgeMetrics };
