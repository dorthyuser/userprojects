using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace BusTravelAccountsSaFunction.Tests;

/// R1 — 204 "(no body)": GET /accounts, GET /accounts/{id} otherwise — not sizeOf(payload) > 0.
public class R1Tests
{
    private readonly IAccountsRepository repository = Substitute.For<IAccountsRepository>();

    [Fact]
    public async Task R1_get_accounts_with_no_matches_answers_204()
    {
        repository.GetByEmailAsync("ana@example.com", Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AccountResponse>());

        var service = new AccountsService(repository, NullLogger<AccountsService>.Instance);
        var function = new AccountsFunctions(service, NullLogger<AccountsFunctions>.Instance);
        var request = new DefaultHttpContext().Request;
        request.Method = "GET";
        request.QueryString = new QueryString("?email=ana%40example.com");

        var (status, body) = Read(await function.GetAccounts(request));

        Assert.Equal(204, status);
        Assert.Equal("", body);
    }

    [Fact]
    public async Task R1_get_account_by_id_with_no_match_answers_204()
    {
        repository.GetByIdAsync("42", Arg.Any<CancellationToken>())
            .Returns((AccountResponse?)null);

        var service = new AccountsService(repository, NullLogger<AccountsService>.Instance);
        var function = new AccountsFunctions(service, NullLogger<AccountsFunctions>.Instance);
        var request = new DefaultHttpContext().Request;
        request.Method = "GET";

        var (status, body) = Read(await function.GetAccountById(request, "42"));

        Assert.Equal(204, status);
        Assert.Equal("", body);
    }

    private static (int Status, string Body) Read(IActionResult result) => result switch
    {
        ContentResult c => (c.StatusCode ?? 200, c.Content ?? ""),
        ObjectResult o => (o.StatusCode ?? 200, JsonSerializer.Serialize(o.Value, JsonDefaults.Options)),
        StatusCodeResult s => (s.StatusCode, ""),
        _ => throw new InvalidOperationException($"Unexpected result {result.GetType().Name}")
    };
}
