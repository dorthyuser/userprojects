using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace BusTravelAccountsSaFunction.Tests;

/// R7 — 401 "{namespace} {identifier} ERROR": every endpoint when Salesforce connectivity error description contains "Invalid".
public class R7Tests
{
    [Fact]
    public async Task R7_get_accounts_with_invalid_salesforce_connectivity_answers_401()
    {
        var repository = Substitute.For<IAccountsRepository>();
        repository.GetAccountsByEmailAsync("ana@example.com", Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<System.Text.Json.Nodes.JsonObject>>(
                new SalesforceException("SALESFORCE", "CONNECTIVITY", "Invalid login: bad credentials")));
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

        var (status, body) = Read(await function.GetAccounts(request));

        Assert.Equal(401, status);
        Assert.Contains("SALESFORCE CONNECTIVITY ERROR", body);
    }

    private static (int Status, string Body) Read(IActionResult result) => result switch
    {
        ContentResult c => (c.StatusCode ?? 200, c.Content ?? ""),
        ObjectResult o => (o.StatusCode ?? 200, JsonSerializer.Serialize(o.Value, JsonDefaults.Options)),
        StatusCodeResult s => (s.StatusCode, ""),
        _ => throw new InvalidOperationException($"Unexpected result {result.GetType().Name}")
    };
}
