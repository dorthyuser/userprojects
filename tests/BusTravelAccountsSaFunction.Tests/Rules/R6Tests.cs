using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace BusTravelAccountsSaFunction.Tests;

/// R6 — 415 "UNSUPPORTED MEDIA TYPE": every endpoint when APIKIT:UNSUPPORTED_MEDIA_TYPE.
public class R6Tests
{
    [Fact]
    public async Task R6_post_accounts_with_non_json_content_type_answers_415()
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
        request.ContentType = "text/plain";
        request.Body = new MemoryStream(Encoding.UTF8.GetBytes("x"));

        var (status, body) = Read(await function.PostAccounts(request));

        Assert.Equal(415, status);
        Assert.Contains("UNSUPPORTED MEDIA TYPE", body);
    }

    [Fact]
    public async Task R6_put_accounts_with_non_json_content_type_answers_415()
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
        request.ContentType = "text/plain";
        request.Body = new MemoryStream(Encoding.UTF8.GetBytes("x"));

        var (status, body) = Read(await function.PutAccounts(request, "42"));

        Assert.Equal(415, status);
        Assert.Contains("UNSUPPORTED MEDIA TYPE", body);
    }

    private static (int Status, string Body) Read(IActionResult result) => result switch
    {
        ContentResult c => (c.StatusCode ?? 200, c.Content ?? ""),
        ObjectResult o => (o.StatusCode ?? 200, JsonSerializer.Serialize(o.Value, JsonDefaults.Options)),
        StatusCodeResult s => (s.StatusCode, ""),
        _ => throw new InvalidOperationException($"Unexpected result {result.GetType().Name}")
    };
}
