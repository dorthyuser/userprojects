using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace BusTravelAccountsSaFunction.Tests;

/// R9 — 500 "{namespace} {identifier} ERROR": every endpoint when any unhandled error.
public class R9Tests
{
    [Fact]
    public async Task R9_get_accounts_with_unhandled_error_escapes_and_unexpected_answers_500()
    {
        var repository = Substitute.For<IAccountsRepository>();
        repository.GetAccountsByEmailAsync("ana@example.com", Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<System.Text.Json.Nodes.JsonObject>>(new InvalidOperationException("boom")));
        var errorAuditClient = Substitute.For<IErrorAuditClient>();
        var function = new AccountsFunctions(
            new AccountsService(repository),
            errorAuditClient,
            NullLogger<AccountsFunctions>.Instance);

        var context = new DefaultHttpContext();
        var request = context.Request;
        request.Method = "GET";
        request.Path = "/accounts";
        request.QueryString = new QueryString("?email=ana%40example.com");

        await Assert.ThrowsAnyAsync<Exception>(() => function.GetAccounts(request));

        var (status, body) = Read(ErrorResults.Unexpected());

        Assert.Equal(500, status);
        Assert.Contains("MULE UNKNOWN ERROR", body);
    }

    private static (int Status, string Body) Read(IActionResult result) => result switch
    {
        ContentResult c => (c.StatusCode ?? 200, c.Content ?? ""),
        ObjectResult o => (o.StatusCode ?? 200, JsonSerializer.Serialize(o.Value, JsonDefaults.Options)),
        StatusCodeResult s => (s.StatusCode, ""),
        _ => throw new InvalidOperationException($"Unexpected result {result.GetType().Name}")
    };
}
