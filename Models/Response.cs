namespace LifeTimeCalculatorLambda.Models;

public sealed class Response
{
    public string dateOfBirth { get; set; } = string.Empty;
    public string currentDate { get; set; } = string.Empty;
    public Age age { get; set; } = new();
    public LifetimeStats lifetimeStats { get; set; } = new();
}

public sealed class Age
{
    public int years { get; set; }
    public int months { get; set; }
    public int weeks { get; set; }
    public int days { get; set; }
    public int hours { get; set; }
    public int minutes { get; set; }
    public int seconds { get; set; }
}

public sealed class LifetimeStats
{
    public long totalYearsLived { get; set; }
    public long totalMonthsLived { get; set; }
    public long totalWeeksLived { get; set; }
    public long totalDaysLived { get; set; }
    public long totalHoursLived { get; set; }
    public long totalMinutesLived { get; set; }
    public long totalSecondsLived { get; set; }
}