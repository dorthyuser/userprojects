using AdverseEventNotificationService.Api.Contracts;
using AdverseEventNotificationService.Api.Domain;
using AdverseEventNotificationService.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace AdverseEventNotificationService.Api.Tests;

public sealed class ServiceTests
{
    [Fact]
    public async Task SubmitAsync_ShouldCoerceSeriousAndFatalOutcome()
    {
        var aeRepo = new Mock<IAdverseEventRepository>();
        var notifRepo = new Mock<INotificationRepository>();
        var idGen = new Mock<IIdGenerator>();
        aeRepo.Setup(x => x.TrialExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        aeRepo.Setup(x => x.PatientEnrolledAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        aeRepo.Setup(x => x.FindDuplicateAeIdAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
        idGen.Setup(x => x.GenerateAeIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync("AE-2026-000001");
        idGen.Setup(x => x.GenerateNotificationIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync("NOTIF-2026-000001");

        var service = new AdverseEventService(aeRepo.Object, notifRepo.Object, idGen.Object, Options.Create(new Configuration.DatabaseOptions()), NullLogger<AdverseEventService>.Instance);
        var response = await service.SubmitAsync(new AdverseEventRequestDto { TrialId = "T1", SiteId = "S1", PatientId = "P1", ClinicianId = "C1", EventDate = DateTimeOffset.UtcNow, AeTermCode = "A1", AeTermName = "Headache", CtcaeGrade = 5, Serious = false, Outcome = AdverseEventOutcome.Unknown, ActionTaken = ActionTaken.None, Narrative = "n", ReportedBy = "a@b.com" }, CancellationToken.None);

        response.AeId.Should().Be("AE-2026-000001");
        aeRepo.Verify(x => x.InsertAsync(It.Is<AdverseEventEntity>(e => e.Serious && e.Outcome == AdverseEventOutcome.Fatal), It.IsAny<CancellationToken>()), Times.Once);
    }
}