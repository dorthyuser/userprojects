namespace life_time_calculator.Models;

public class LifetimeCalculationResult
{
    public string DateOfBirth { get; set; } = string.Empty;
    public string CurrentDate { get; set; } = string.Empty;
    public AgeBreakdown Age { get; set; } = new AgeBreakdown();
    public LifetimeStats LifetimeStats { get; set; } = new LifetimeStats();
}
