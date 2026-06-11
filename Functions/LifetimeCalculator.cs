using System;
using life_time_calculator.Models;

namespace life_time_calculator.Functions;

public static class LifetimeCalculator
{
    public static LifetimeCalculationResult Calculate(DateTimeOffset dateOfBirth, DateTimeOffset currentDate)
    {
        TimeSpan elapsed = currentDate - dateOfBirth;
        DateTimeOffset cursor = dateOfBirth;

        int years = 0;
        while (cursor.AddYears(1) <= currentDate)
        {
            cursor = cursor.AddYears(1);
            years++;
        }

        int months = 0;
        while (cursor.AddMonths(1) <= currentDate)
        {
            cursor = cursor.AddMonths(1);
            months++;
        }

        TimeSpan remainder = currentDate - cursor;
        int weeks = (int)(remainder.TotalDays / 7);
        remainder = remainder.Subtract(TimeSpan.FromDays(weeks * 7));

        int days = remainder.Days;
        remainder = remainder.Subtract(TimeSpan.FromDays(days));

        int hours = remainder.Hours;
        remainder = remainder.Subtract(TimeSpan.FromHours(hours));

        int minutes = remainder.Minutes;
        remainder = remainder.Subtract(TimeSpan.FromMinutes(minutes));

        int seconds = remainder.Seconds;

        return new LifetimeCalculationResult
        {
            DateOfBirth = dateOfBirth.UtcDateTime.ToString("O"),
            CurrentDate = currentDate.UtcDateTime.ToString("O"),
            Age = new AgeBreakdown
            {
                Years = years,
                Months = months,
                Weeks = weeks,
                Days = days,
                Hours = hours,
                Minutes = minutes,
                Seconds = seconds
            },
            LifetimeStats = new LifetimeStats
            {
                TotalYearsLived = elapsed.TotalDays / 365.2425,
                TotalMonthsLived = elapsed.TotalDays / 30.436875,
                TotalWeeksLived = elapsed.TotalDays / 7d,
                TotalDaysLived = elapsed.TotalDays,
                TotalHoursLived = elapsed.TotalHours,
                TotalMinutesLived = elapsed.TotalMinutes,
                TotalSecondsLived = elapsed.TotalSeconds
            }
        };
    }
}
