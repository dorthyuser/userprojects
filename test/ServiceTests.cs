// GENERATED_BY_AI_TEST_ENGINE
using System;
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
        var sut = new Service();
        var request = new Request
        {
            dateOfBirth = DateTimeOffset.UtcNow.AddYears(-10).AddMonths(-2).AddDays(-3).ToString("o")
        };

        var result = await sut.CalculateAsync(request);

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.dateOfBirth));
        Assert.False(string.IsNullOrWhiteSpace(result.currentDate));
        Assert.NotNull(result.age);
        Assert.NotNull(result.lifetimeStats);
        Assert.True(result.age.years >= 10);
        Assert.True(result.age.months >= 0);
        Assert.True(result.lifetimeStats.totalDaysLived > 0);
    }

    [Fact]
    public async Task CalculateAsync_ReturnsZeroOrPositiveAge_WhenDateOfBirthIsToday()
    {
        var sut = new Service();
        var now = DateTimeOffset.UtcNow;
        var request = new Request
        {
            dateOfBirth = now.ToString("o")
        };

        var result = await sut.CalculateAsync(request);

        Assert.NotNull(result);
        Assert.Equal(0, result.age.years);
        Assert.True(result.age.months >= 0);
        Assert.True(result.age.weeks >= 0);
        Assert.True(result.lifetimeStats.totalSecondsLived >= 0);
    }

    [Fact]
    public async Task CalculateAsync_ThrowsArgumentNullException_WhenRequestIsNull()
    {
        var sut = new Service();

        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => sut.CalculateAsync(null!));

        Assert.Equal("request", exception.ParamName);
    }

    [Fact]
    public async Task CalculateAsync_ThrowsArgumentException_WhenDateOfBirthIsMissing()
    {
        var sut = new Service();
        var request = new Request
        {
            dateOfBirth = ""
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => sut.CalculateAsync(request));

        Assert.Equal("dateOfBirth is required.", exception.Message);
    }

    [Fact]
    public async Task CalculateAsync_ThrowsArgumentException_WhenDateOfBirthFormatIsInvalid()
    {
        var sut = new Service();
        var request = new Request
        {
            dateOfBirth = "not-a-date"
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => sut.CalculateAsync(request));

        Assert.Equal("Invalid dateOfBirth format.", exception.Message);
    }

    [Fact]
    public async Task CalculateAsync_ThrowsArgumentException_WhenDateOfBirthIsInFuture()
    {
        var sut = new Service();
        var request = new Request
        {
            dateOfBirth = DateTimeOffset.UtcNow.AddDays(1).ToString("o")
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => sut.CalculateAsync(request));

        Assert.Equal("dateOfBirth cannot be in the future.", exception.Message);
    }
}