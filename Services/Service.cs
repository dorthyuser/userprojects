using System.Globalization;
using LifeTimeCalculatorLambda.Models;

namespace LifeTimeCalculatorLambda.Services;

public sealed class Service
{
    public Task<Response> CalculateAsync(Request request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.dateOfBirth))
        {
            throw new ArgumentException("dateOfBirth is required.");
        }

        if (!DateTimeOffset.TryParse(request.dateOfBirth, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dob))
        {
            throw new ArgumentException("Invalid dateOfBirth format.");
        }

        var now = DateTimeOffset.UtcNow;
        if (dob > now)
        {
            throw new ArgumentException("dateOfBirth cannot be in the future.");
        }

        var timespan = now - dob;
        var years = now.Year - dob.Year;
        var months = (now.Year - dob.Year) * 12 + now.Month - dob.Month;
        if (now.Day < dob.Day) months--;
        if (months < 0) months = 0;
        if (years < 0) years = 0;

        var response = new Response
        {
            dateOfBirth = dob.ToString("o"),
            currentDate = now.ToString("o"),
            age = new Age
            {
                years = years,
                months = months,
                weeks = (int)Math.Floor(timespan.TotalDays / 7d),
                days = (int)Math.Floor(timespan.TotalDays),
                hours = (int)Math.Floor(timespan.TotalHours),
                minutes = (int)Math.Floor(timespan.TotalMinutes),
                seconds = (int)Math.Floor(timespan.TotalSeconds)
            },
            lifetimeStats = new LifetimeStats
            {
                totalYearsLived = (long)Math.Floor(timespan.TotalDays / 365.2425d),
                totalMonthsLived = (long)Math.Floor(timespan.TotalDays / 30.436875d),
                totalWeeksLived = (long)Math.Floor(timespan.TotalDays / 7d),
                totalDaysLived = (long)Math.Floor(timespan.TotalDays),
                totalHoursLived = (long)Math.Floor(timespan.TotalHours),
                totalMinutesLived = (long)Math.Floor(timespan.TotalMinutes),
                totalSecondsLived = (long)Math.Floor(timespan.TotalSeconds)
            }
        };

        return Task.FromResult(response);
    }
}