using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace BusTravelAccountsSaFunction.Tests;

/// R8 — 500 "MULE UNKNOWN ERROR": POST /accounts, PUT /accounts/{id} empty request body on POST or PUT.
public class R8Tests
{
    private readonly IAccountsRepository repository = Substitute.For<IAccountsRepository>();

    [Fact]
    public async Task R8_post_accounts_with_empty_body_answers_500()
    {
        var service = new AccountsService(repository, NullLogger<AccountsService>.Instance);
        var function = new AccountsFunctions(service, NullLogger<AccountsFunctions>.Instance);
        var context = new DefaultHttpContext();
        var request = context.Request;
        request.Method = "POST";
        request.ContentType = "application/json";
        request.ContentLength = 0;
        request.Body = new MemoryStream();

        var (status, body) = Read(await function.PostAccounts(request));

        Assert.Equal(500, status);
        Assert.Contains("MULE UNKNOWN ERROR", body);
    }

    [Fact]
    public async Task R8_put_accounts_with_empty_body_answers_500()
    {
        var service = new AccountsService(repository, NullLogger<AccountsService>.Instance);
        var function = new AccountsFunctions(service, NullLogger<AccountsFunctions>.Instance);
        var context = new DefaultHttpContext();
        var request = context.Request;
        request.Method = "PUT";
        request.ContentType = "application/json";
        request.ContentLength = 0;
        request.Body = new MemoryStream();

        var (status, body) = Read(await function.PutAccounts(request, "11111111-1111-1111-1111-111111111111"));

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
