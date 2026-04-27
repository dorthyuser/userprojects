// GENERATED_BY_AI_TEST_ENGINE
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using test_sf_git_prop.Controllers;
using test_sf_git_prop.Models;
using test_sf_git_prop.Services;
using Xunit;

namespace test_sf_git_prop.Tests;

public class AccountsControllerTests
{
    [Fact]
    public async Task GetAccounts_ReturnsOkResult_WhenServiceSucceeds()
    {
        var accounts = new List<AccountDto>
        {
            new AccountDto { Name = "Acme" }
        };

        var mockService = new Mock<IAccountService>();
        mockService
            .Setup(s => s.GetAccountsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(accounts);

        var mockLogger = new Mock<ILogger<AccountsController>>();
        var controller = new AccountsController(mockService.Object, mockLogger.Object);

        var result = await controller.GetAccounts(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = Assert.IsAssignableFrom<IEnumerable<AccountDto>>(okResult.Value);
        Assert.Single(value);
        mockService.Verify(s => s.GetAccountsAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task GetAccounts_ReturnsInternalServerError_WhenServiceThrows()
    {
        var mockService = new Mock<IAccountService>();
        mockService
            .Setup(s => s.GetAccountsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("failure"));

        var mockLogger = new Mock<ILogger<AccountsController>>();
        var controller = new AccountsController(mockService.Object, mockLogger.Object);

        var result = await controller.GetAccounts(CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);
        mockService.Verify(s => s.GetAccountsAsync(It.IsAny<CancellationToken>()), Times.Once());
    }
}
