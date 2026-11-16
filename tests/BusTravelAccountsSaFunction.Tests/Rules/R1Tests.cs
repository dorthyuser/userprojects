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
    [Fact]
    public async Task R1_get_accounts_with_no_results_answers_204()
    {
        var repository = Substitute.For<IAccountsRepository>();
        repository.GetAccountsByEmailAsync("ana@example.com", Arg.Any<CancellationToken>())
            .Returns(Array.Empty<System.Text.Json.Nodes.JsonObject>());
        var errorAuditClient = Substitute.For<IErrorAuditClient>();
        var function = new AccountsFunctions(
            new AccountsService(repository),
            errorAuditClient,
            NullLogger<AccountsFunctions>.Instance);

        var context = new DefaultHttpContext();
        var request = context.Request;
        request.Method = "GET";
        request.QueryString = new QueryString("?email=ana%40example.com");

        var (status, body) = Read(await function.GetAccounts(request));

        Assert.Equal(204, status);
        Assert.Equal("", body);
    }

    [Fact]
    public async Task R1_get_account_by_id_with_no_result_answers_204()
    {
        var repository = Substitute.For<IAccountsRepository>();
        repository.GetAccountByIdAsync("42", Arg.Any<CancellationToken>())
            .Returns(Array.Empty<System.Text.Json.Nodes.JsonObject>());
        var errorAuditClient = Substitute.For<IErrorAuditClient>();
        var function = new AccountsFunctions(
            new AccountsService(repository),
            errorAuditClient,
            NullLogger<AccountsFunctions>.Instance);

        var context = new DefaultHttpContext();
        var request = context.Request;
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
