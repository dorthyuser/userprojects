// GENERATED_BY_AI_TEST_ENGINE
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using test_sf_git_prop.Models;
using test_sf_git_prop.Services;
using Xunit;

namespace test_sf_git_prop.Tests;

public class AccountServiceTests
{
    [Fact]
    public async Task GetAccountsAsync_ReturnsAccounts_FromSalesforceClient()
    {
        var expected = new List<AccountDto>
        {
            new AccountDto()
        };

        var mockClient = new Mock<ISalesforceClient>();
        mockClient
            .Setup(c => c.GetAccountsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var sut = new AccountService(mockClient.Object);

        var result = await sut.GetAccountsAsync(CancellationToken.None);

        Assert.Same(expected, result);
        mockClient.Verify(c => c.GetAccountsAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task GetAccountsAsync_ReturnsAccounts_EmptyCollection()
    {
        var expected = Array.Empty<AccountDto>();

        var mockClient = new Mock<ISalesforceClient>();
        mockClient
            .Setup(c => c.GetAccountsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var sut = new AccountService(mockClient.Object);

        var result = await sut.GetAccountsAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result);
        mockClient.Verify(c => c.GetAccountsAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task CreateAccountAsync_ReturnsCreatedAccount_WhenNameIsValid()
    {
        var request = new CreateAccountRequest
        {
            Name = "Test Account"
        };
        var expected = new AccountDto();

        var mockClient = new Mock<ISalesforceClient>();
        mockClient
            .Setup(c => c.CreateAccountAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var sut = new AccountService(mockClient.Object);

        var result = await sut.CreateAccountAsync(request, CancellationToken.None);

        Assert.Same(expected, result);
        mockClient.Verify(c => c.CreateAccountAsync(request, It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task CreateAccountAsync_ThrowsArgumentException_WhenNameIsMissing()
    {
        var request = new CreateAccountRequest
        {
            Name = string.Empty
        };

        var mockClient = new Mock<ISalesforceClient>(MockBehavior.Strict);
        var sut = new AccountService(mockClient.Object);

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => sut.CreateAccountAsync(request, CancellationToken.None));

        Assert.Equal("Name is required.", exception.Message);
    }

    [Fact]
    public async Task UpdateAccountAsync_ReturnsUpdatedAccount_WhenIdIsValid()
    {
        var id = "001";
        var request = new UpdateAccountRequest();
        var expected = new AccountDto();

        var mockClient = new Mock<ISalesforceClient>();
        mockClient
            .Setup(c => c.UpdateAccountAsync(id, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var sut = new AccountService(mockClient.Object);

        var result = await sut.UpdateAccountAsync(id, request, CancellationToken.None);

        Assert.Same(expected, result);
        mockClient.Verify(c => c.UpdateAccountAsync(id, request, It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task UpdateAccountAsync_ThrowsArgumentException_WhenIdIsMissing()
    {
        var request = new UpdateAccountRequest();
        var mockClient = new Mock<ISalesforceClient>(MockBehavior.Strict);
        var sut = new AccountService(mockClient.Object);

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => sut.UpdateAccountAsync(string.Empty, request, CancellationToken.None));

        Assert.Equal("Account id is required.", exception.Message);
    }
}
