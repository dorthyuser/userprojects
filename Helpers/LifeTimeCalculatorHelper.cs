using System;
using LifeTimeCalculator.Models;

namespace LifeTimeCalculator.Helpers;

public static class LifeTimeCalculatorHelper
{
    public static LifeTimeCalculationResponse Calculate(DateTimeOffset dateOfBirth, DateTimeOffset currentDate)
    {
        var dobUtc = dateOfBirth.UtcDateTime;
        var nowUtc = currentDate.UtcDateTime;
        var totalSeconds = (nowUtc - dobUtc).TotalSeconds;
        var totalMinutes = (nowUtc - dobUtc).TotalMinutes;
        var totalHours = (nowUtc - dobUtc).TotalHours;
        var totalDays = (nowUtc - dobUtc).TotalDays;
        var totalWeeks = totalDays / 7d;
        var totalMonths = totalDays / 30.436875d;
        var totalYears = totalDays / 365.2425d;

        var years = nowUtc.Year - dobUtc.Year;
        var months = nowUtc.Month - dobUtc.Month;
        var days = nowUtc.Day - dobUtc.Day;
        var hours = nowUtc.Hour - dobUtc.Hour;
        var minutes = nowUtc.Minute - dobUtc.Minute;
        var seconds = nowUtc.Second - dobUtc.Second;

        if (seconds < 0)
        {
            seconds += 60;
            minutes--;
        }

        if (minutes < 0)
        {
            minutes += 60;
            hours--;
        }

        if (hours < 0)
        {
            hours += 24;
            days--;
        }

        if (days < 0)
        {
            var previousMonth = nowUtc.AddMonths(-1);
            days += DateTime.DaysInMonth(previousMonth.Year, previousMonth.Month);
            months--;
        }

        if (months < 0)
        {
            months += 12;
            years--;
        }

        return new LifeTimeCalculationResponse
        {
            DateOfBirth = dateOfBirth.UtcDateTime.ToString("O"),
            CurrentDate = currentDate.UtcDateTime.ToString("O"),
            Age = new AgeBreakdown
            {
                Years = Math.Max(years, 0),
                Months = Math.Max(months, 0),
                Weeks = (int)Math.Floor(totalWeeks),
                Days = (int)Math.Floor(totalDays),
                Hours = (int)Math.Floor(totalHours),
                Minutes = (int)Math.Floor(totalMinutes),
                Seconds = (int)Math.Floor(totalSeconds)
            },
            LifetimeStats = new LifetimeStats
            {
                TotalYearsLived = Math.Round(totalYears, 6),
                TotalMonthsLived = Math.Round(totalMonths, 6),
                TotalWeeksLived = Math.Round(totalWeeks, 6),
                TotalDaysLived = Math.Round(totalDays, 6),
                TotalHoursLived = Math.Round(totalHours, 6),
                TotalMinutesLived = Math.Round(totalMinutes, 6),
                TotalSecondsLived = Math.Round(totalSeconds, 6)
            }
        };
    }
}
