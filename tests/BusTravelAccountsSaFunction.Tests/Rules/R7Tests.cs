using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace BusTravelAccountsSaFunction.Tests;

/// R7 — 500 "{namespace} {identifier} ERROR": every endpoint unhandled error reaches global ANY handler.
public class R7Tests
{
    private readonly IAccountsRepository repository = Substitute.For<IAccountsRepository>();

    [Fact]
    public async Task R7_unhandled_error_reaches_global_handler_but_unexpected_result_does_not_match_rule_text()
    {
        repository.GetByEmailAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<AccountRecord>>(new InvalidOperationException("boom")));

        var service = new AccountsService(repository, NullLogger<AccountsService>.Instance);
        var function = new AccountsFunctions(service, NullLogger<AccountsFunctions>.Instance);
        var request = new DefaultHttpContext().Request;
        request.Method = "GET";

        await Assert.ThrowsAnyAsync<Exception>(() => function.GetAccounts(request));

        var (status, body) = Read(ErrorResults.Unexpected());

        Assert.Equal(500, status);
        Assert.Contains("MULE UNKNOWN ERROR", body);
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
