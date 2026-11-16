using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace BusTravelAccountsSaFunction.Tests;

/// R8 — 404 "{namespace} {identifier} ERROR": every endpoint when Salesforce connectivity error description contains "Failed to send request".
public class R8Tests
{
    [Fact]
    public async Task R8_get_account_by_id_with_failed_to_send_request_answers_404()
    {
        var repository = Substitute.For<IAccountsRepository>();
        repository.GetAccountByIdAsync("42", Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<System.Text.Json.Nodes.JsonObject>>(
                new SalesforceException("SALESFORCE", "CONNECTIVITY", "Failed to send request")));
        var errorAuditClient = Substitute.For<IErrorAuditClient>();
        var function = new AccountsFunctions(
            new AccountsService(repository),
            errorAuditClient,
            NullLogger<AccountsFunctions>.Instance);

        var context = new DefaultHttpContext();
        var request = context.Request;
        request.Method = "GET";
        request.Path = "/accounts/42";

        var (status, body) = Read(await function.GetAccountById(request, "42"));

        Assert.Equal(404, status);
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
