using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace BusTravelAccountsSaFunction.Tests;

/// R6 — 415 "UNSUPPORTED MEDIA TYPE": every endpoint known body endpoint with unsupported Content-Type.
public class R6Tests
{
    private readonly IAccountsRepository repository = Substitute.For<IAccountsRepository>();

    [Fact]
    public async Task R6_post_accounts_with_unsupported_content_type_answers_415()
    {
        var service = new AccountsService(repository, NullLogger<AccountsService>.Instance);
        var function = new AccountsFunctions(service, NullLogger<AccountsFunctions>.Instance);
        var request = new DefaultHttpContext().Request;
        request.Method = "POST";
        request.ContentType = "text/plain";
        request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{}"));

        var (status, body) = Read(await function.PostAccounts(request));

        Assert.Equal(415, status);
        Assert.Contains("UNSUPPORTED MEDIA TYPE", body);
    }

    [Fact]
    public async Task R6_put_accounts_with_unsupported_content_type_answers_415()
    {
        var service = new AccountsService(repository, NullLogger<AccountsService>.Instance);
        var function = new AccountsFunctions(service, NullLogger<AccountsFunctions>.Instance);
        var request = new DefaultHttpContext().Request;
        request.Method = "PUT";
        request.ContentType = "text/plain";
        request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{}"));

        var (status, body) = Read(await function.PutAccounts(request, "11111111-1111-1111-1111-111111111111"));

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
