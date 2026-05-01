// GENERATED_BY_AI_TEST_ENGINE
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using new_test_for_demo.Models;
using new_test_for_demo.Services;
using Xunit;

namespace new_test_for_demo.Tests;

public class TravelcardServiceTests
{
    [Fact]
    public async Task CreateAsync_ThrowsArgumentException_WhenRequestedDateIsNotInPast()
    {
        var loggerMock = new Mock<ILogger<TravelcardService>>();
        var service = CreateService(loggerMock.Object);

        var request = BuildValidRequest();
        request.TravelcardRequestedDate = DateTimeOffset.UtcNow.AddDays(1);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request));

        Assert.Contains("requestedDate must be in the past", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_ThrowsArgumentException_WhenValidFromIsAfterOrEqualValidTo()
    {
        var loggerMock = new Mock<ILogger<TravelcardService>>();
        var service = CreateService(loggerMock.Object);

        var request = BuildValidRequest();
        request.TravelcardValidFrom = DateTimeOffset.UtcNow.AddDays(-1);
        request.TravelcardValidTo = request.TravelcardValidFrom;

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request));

        Assert.Contains("validFrom must be before validTo", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_ThrowsArgumentException_WhenValidToIsNotInFuture()
    {
        var loggerMock = new Mock<ILogger<TravelcardService>>();
        var service = CreateService(loggerMock.Object);

        var request = BuildValidRequest();
        request.TravelcardValidTo = DateTimeOffset.UtcNow.AddMinutes(-1);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request));

        Assert.Contains("validTo must be in the future", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_ThrowsArgumentException_WhenSixteenToSeventeenMissingUsableTo()
    {
        var loggerMock = new Mock<ILogger<TravelcardService>>();
        var service = CreateService(loggerMock.Object);

        var request = BuildValidRequest();
        request.TravelcardType = TravelcardTypeEnum.SixteenToSeventeen;
        request.TravelcardUsableTo = null;

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request));

        Assert.Contains("usableTo is required for SixteenToSeventeen", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_ThrowsArgumentException_WhenUsableToIsNotInFuture()
    {
        var loggerMock = new Mock<ILogger<TravelcardService>>();
        var service = CreateService(loggerMock.Object);

        var request = BuildValidRequest();
        request.TravelcardUsableTo = DateTimeOffset.UtcNow.AddMinutes(-1);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request));

        Assert.Contains("usableTo must be in the future", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_ThrowsArgumentException_WhenSecondaryCardholderNotAllowed()
    {
        var loggerMock = new Mock<ILogger<TravelcardService>>();
        var service = CreateService(loggerMock.Object);

        var request = BuildValidRequest();
        request.TravelcardType = TravelcardTypeEnum.Veterans;
        request.Cardholders = new List<CardholderRequest>
        {
            BuildCardholder(CardholderTypeEnum.Secondary)
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request));

        Assert.Contains("Secondary cardholder not allowed for this travelcard type", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_ThrowsArgumentException_WhenCardholderPhotoSelectionIsInvalid()
    {
        var loggerMock = new Mock<ILogger<TravelcardService>>();
        var service = CreateService(loggerMock.Object);

        var request = BuildValidRequest();
        request.Cardholders = new List<CardholderRequest>
        {
            new CardholderRequest
            {
                CardholderType = CardholderTypeEnum.Primary,
                CardholderPhotoKey = "k1",
                CardholderPhotoURL = "u1"
            }
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request));

        Assert.Contains("Each cardholder must provide exactly one", ex.Message);
    }

    private static TravelcardService CreateService(ILogger<TravelcardService> logger)
    {
        Environment.SetEnvironmentVariable("POSTGRESQL_HOST", "localhost");
        Environment.SetEnvironmentVariable("POSTGRESQL_PORT", "5432");
        Environment.SetEnvironmentVariable("POSTGRESQL_DATABASE", "testdb");
        Environment.SetEnvironmentVariable("POSTGRESQL_USERNAME", "testuser");
        Environment.SetEnvironmentVariable("POSTGRESQL_PASSWORD", "testpassword");
        return new TravelcardService(logger);
    }

    private static CreateTravelcardRequest BuildValidRequest()
    {
        return new CreateTravelcardRequest
        {
            TravelcardType = TravelcardTypeEnum.Young,
            TravelcardRequestedDate = DateTimeOffset.UtcNow.AddDays(-2),
            TravelcardValidFrom = DateTimeOffset.UtcNow.AddDays(-1),
            TravelcardValidTo = DateTimeOffset.UtcNow.AddDays(10),
            TravelcardUsableTo = DateTimeOffset.UtcNow.AddDays(10),
            Cardholders = new List<CardholderRequest>
            {
                BuildCardholder(CardholderTypeEnum.Primary)
            }
        };
    }

    private static CardholderRequest BuildCardholder(CardholderTypeEnum type)
    {
        return new CardholderRequest
        {
            CardholderType = type,
            CardholderPhotoKey = "photo-key"
        };
    }
}