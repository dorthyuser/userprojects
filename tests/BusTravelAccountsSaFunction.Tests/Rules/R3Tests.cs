using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace BusTravelAccountsSaFunction.Tests;

/// R3 — 400 "BAD REQUEST": every endpoint when APIKIT:BAD_REQUEST.
public class R3Tests
{
    [Fact]
    public async Task R3_post_accounts_with_invalid_json_answers_400()
    {
        var repository = Substitute.For<IAccountsRepository>();
        var errorAuditClient = Substitute.For<IErrorAuditClient>();
        var function = new AccountsFunctions(
            new AccountsService(repository),
            errorAuditClient,
            NullLogger<AccountsFunctions>.Instance);

        var context = new DefaultHttpContext();
        var request = context.Request;
        request.Method = "POST";
        request.Path = "/accounts";
        request.ContentType = "application/json";
        request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{"));

        var (status, body) = Read(await function.PostAccounts(request));

        Assert.Equal(400, status);
        Assert.Contains("BAD REQUEST", body);
    }

    [Fact]
    public async Task R3_put_accounts_with_invalid_json_answers_400()
    {
        var repository = Substitute.For<IAccountsRepository>();
        var errorAuditClient = Substitute.For<IErrorAuditClient>();
        var function = new AccountsFunctions(
            new AccountsService(repository),
            errorAuditClient,
            NullLogger<AccountsFunctions>.Instance);

        var context = new DefaultHttpContext();
        var request = context.Request;
        request.Method = "PUT";
        request.Path = "/accounts/42";
        request.ContentType = "application/json";
        request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{"));

        var (status, body) = Read(await function.PutAccounts(request, "42"));

        Assert.Equal(400, status);
        Assert.Contains("BAD REQUEST", body);
    }

    private static (int Status, string Body) Read(IActionResult result) => result switch
    {
        ContentResult c => (c.StatusCode ?? 200, c.Content ?? ""),
        ObjectResult o => (o.StatusCode ?? 200, JsonSerializer.Serialize(o.Value, JsonDefaults.Options)),
        StatusCodeResult s => (s.StatusCode, ""),
        _ => throw new InvalidOperationException($"Unexpected result {result.GetType().Name}")
    };
}
