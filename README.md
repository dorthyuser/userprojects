LifeTimeCalculator Lambda

This project implements an AWS Lambda function in .NET 8 that calculates elapsed time from a provided date of birth to the current UTC moment.

Input:
{
  "dateOfBirth": "<ISO-8601 datetime>"
}

Output:
{
  "dateOfBirth": "...",
  "currentDate": "...",
  "age": {
    "years": 0,
    "months": 0,
    "weeks": 0,
    "days": 0,
    "hours": 0,
    "minutes": 0,
    "seconds": 0
  },
  "lifetimeStats": {
    "totalYearsLived": 0,
    "totalMonthsLived": 0,
    "totalWeeksLived": 0,
    "totalDaysLived": 0,
    "totalHoursLived": 0,
    "totalMinutesLived": 0,
    "totalSecondsLived": 0
  }
}

The project uses environment-variable-based configuration and is set up for AWS Lambda deployment with .NET 8.
