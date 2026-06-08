// GENERATED_BY_AI_TEST_ENGINE
using Xunit;
using LifeTimeCalculator.Models;

namespace LifeTimeCalculator.Tests.Models;

public class AgeBreakdownTests
{
    [Fact]
    public void DefaultConstructor_WhenCreated_SetsPropertiesToZero()
    {
        // Arrange & Act
        var model = new AgeBreakdown();

        // Assert
        Assert.Equal(0, model.Years);
        Assert.Equal(0, model.Months);
        Assert.Equal(0, model.Weeks);
        Assert.Equal(0, model.Days);
        Assert.Equal(0, model.Hours);
        Assert.Equal(0, model.Minutes);
        Assert.Equal(0, model.Seconds);
    }

    [Fact]
    public void PropertySetters_WhenAssigned_PersistValues()
    {
        // Arrange
        var years = 10;
        var months = 5;
        var weeks = 4;
        var days = 3;
        var hours = 2;
        var minutes = 1;
        var seconds = 30;

        // Act
        var model = new AgeBreakdown
        {
            Years = years,
            Months = months,
            Weeks = weeks,
            Days = days,
            Hours = hours,
            Minutes = minutes,
            Seconds = seconds
        };

        // Assert
        Assert.Equal(years, model.Years);
        Assert.Equal(months, model.Months);
        Assert.Equal(weeks, model.Weeks);
        Assert.Equal(days, model.Days);
        Assert.Equal(hours, model.Hours);
        Assert.Equal(minutes, model.Minutes);
        Assert.Equal(seconds, model.Seconds);
    }

    [Fact]
    public void PropertySetters_WhenNegativeValuesAssigned_PersistValues()
    {
        // Arrange
        var years = -1;
        var months = -2;
        var weeks = -3;
        var days = -4;
        var hours = -5;
        var minutes = -6;
        var seconds = -7;

        // Act
        var model = new AgeBreakdown
        {
            Years = years,
            Months = months,
            Weeks = weeks,
            Days = days,
            Hours = hours,
            Minutes = minutes,
            Seconds = seconds
        };

        // Assert
        Assert.Equal(years, model.Years);
        Assert.Equal(months, model.Months);
        Assert.Equal(weeks, model.Weeks);
        Assert.Equal(days, model.Days);
        Assert.Equal(hours, model.Hours);
        Assert.Equal(minutes, model.Minutes);
        Assert.Equal(seconds, model.Seconds);
    }
}