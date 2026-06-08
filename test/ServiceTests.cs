// GENERATED_BY_AI_TEST_ENGINE
using System;
using System.Globalization;
using System.Threading.Tasks;
using LifeTimeCalculatorLambda.Models;
using LifeTimeCalculatorLambda.Services;
using Xunit;

namespace LifeTimeCalculatorLambda.Tests;

public class ServiceTests
{
    [Fact]
    public async Task CalculateAsync_ReturnsExpectedResponse_ForValidDateOfBirth()
    {
        // Arrange
        var sut = new Service();
        var dob = DateTimeOffset.UtcNow.AddYears(-30).AddMonths(-2).AddDays(-10);
        var request = new Request
        {
            dateOfBirth = dob.ToString("o", CultureInfo.InvariantCulture)
        };

        // Act
        var result = await sut.CalculateAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dob.ToString("o", CultureInfo.InvariantCulture), result.dateOfBirth);
        Assert.NotNull(result.currentDate);
        Assert.NotNull(result.age);
        Assert.NotNull(result.lifetimeStats);
        Assert.True(result.age.years >= 30);
        Assert.True(result.age.months >= 0);
        Assert.True(result.age.weeks >= 0);
        Assert.True(result.age.days >= 0);
        Assert.True(result.age.hours >= 0);
        Assert.True(result.age.minutes >= 0);
        Assert.True(result.age.seconds >= 0);
        Assert.True(result.lifetimeStats.totalYearsLived >= 30);
        Assert.True(result.lifetimeStats.totalMonthsLived >= 0);
        Assert.True(result.lifetimeStats.totalWeeksLived >= 0);
        Assert.True(result.lifetimeStats.totalDaysLived >= 0);
        Assert.True(result.lifetimeStats.totalHoursLived >= 0);
        Assert.True(result.lifetimeStats.totalMinutesLived >= 0);
        Assert.True(result.lifetimeStats.totalSecondsLived >= 0);
    }

    [Fact]
    public async Task CalculateAsync_ReturnsZeroOrPositiveAge_ForBirthDateInCurrentMonth()
    {
        // Arrange
        var sut = new Service();
        var dob = DateTimeOffset.UtcNow.AddYears(-1).AddDays(1);
        var request = new Request
        {
            dateOfBirth = dob.ToString("o", CultureInfo.InvariantCulture)
        };

        // Act
        var result = await sut.CalculateAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.age);
        Assert.True(result.age.years >= 0);
        Assert.True(result.age.months >= 0);
        Assert.True(result.age.days >= 0);
    }

    [Fact]
    public async Task CalculateAsync_ThrowsArgumentNullException_WhenRequestIsNull()
    {
        // Arrange
        var sut = new Service();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => sut.CalculateAsync(null!));
        Assert.Equal("request", exception.ParamName);
    }

    [Fact]
    public async Task CalculateAsync_ThrowsArgumentException_WhenDateOfBirthIsMissing()
    {
        // Arrange
        var sut = new Service();
        var request = new Request
        {
            dateOfBirth = ""
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => sut.CalculateAsync(request));
        Assert.Equal("dateOfBirth is required.", exception.Message);
    }

    [Fact]
    public async Task CalculateAsync_ThrowsArgumentException_WhenDateOfBirthIsInvalid()
    {
        // Arrange
        var sut = new Service();
        var request = new Request
        {
            dateOfBirth = "not-a-date"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => sut.CalculateAsync(request));
        Assert.Equal("Invalid dateOfBirth format.", exception.Message);
    }

    [Fact]
    public async Task CalculateAsync_ThrowsArgumentException_WhenDateOfBirthIsInFuture()
    {
        // Arrange
        var sut = new Service();
        var request = new Request
        {
            dateOfBirth = DateTimeOffset.UtcNow.AddDays(1).ToString("o", CultureInfo.InvariantCulture)
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => sut.CalculateAsync(request));
        Assert.Equal("dateOfBirth cannot be in the future.", exception.Message);
    }
}