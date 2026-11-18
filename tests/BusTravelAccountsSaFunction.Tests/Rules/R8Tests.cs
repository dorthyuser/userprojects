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
    public async Task R8_post_with_empty_body_answers_500()
    {
        var service = new AccountsService(repository, NullLogger<AccountsService>.Instance);
        var function = new AccountsFunctions(service, NullLogger<AccountsFunctions>.Instance);
        var request = new DefaultHttpContext().Request;
        request.Method = "POST";
        request.ContentType = "application/json";
        request.Body = new MemoryStream();
        request.ContentLength = 0;

        var (status, body) = Read(await function.PostAccounts(request));

        Assert.Equal(500, status);
        Assert.Contains("MULE UNKNOWN ERROR", body);
    }

    [Fact]
    public async Task R8_put_with_empty_body_answers_500()
    {
        var service = new AccountsService(repository, NullLogger<AccountsService>.Instance);
        var function = new AccountsFunctions(service, NullLogger<AccountsFunctions>.Instance);
        var request = new DefaultHttpContext().Request;
        request.Method = "PUT";
        request.ContentType = "application/json";
        request.Body = new MemoryStream();
        request.ContentLength = 0;

        var (status, body) = Read(await function.PutAccounts(request, "42"));

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
