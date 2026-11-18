using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using NSubstitute;
using Xunit;

namespace BusTravelAccountsSaFunction.Tests;

/// R2 — 400 "{SQLState} ERROR": POST /accounts, PUT /accounts/{id} database insert or update fails.
public class R2Tests
{
    private readonly IAccountsRepository repository = Substitute.For<IAccountsRepository>();

    [Fact]
    public async Task R2_post_accounts_when_insert_fails_answers_400_with_sqlstate_error()
    {
        repository.CreateAsync(Arg.Any<CreateAccountRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<Guid>(new PostgresException("insert failed", "ERROR", "ERROR", "23505")));

        var service = new AccountsService(repository, NullLogger<AccountsService>.Instance);
        var function = new AccountsFunctions(service, NullLogger<AccountsFunctions>.Instance);
        var request = new DefaultHttpContext().Request;
        request.Method = "POST";
        request.ContentType = "application/json";
        request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"firstName\":\"Ana\"}"));

        var (status, body) = Read(await function.PostAccounts(request));

        Assert.Equal(400, status);
        Assert.Contains("23505 ERROR", body);
    }

    [Fact]
    public async Task R2_put_accounts_when_update_fails_answers_400_with_sqlstate_error()
    {
        var id = "11111111-1111-1111-1111-111111111111";
        repository.UpdateAsync(Guid.Parse(id), Arg.Any<UpdateAccountRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<Guid>(new PostgresException("update failed", "ERROR", "ERROR", "23503")));

        var service = new AccountsService(repository, NullLogger<AccountsService>.Instance);
        var function = new AccountsFunctions(service, NullLogger<AccountsFunctions>.Instance);
        var request = new DefaultHttpContext().Request;
        request.Method = "PUT";
        request.ContentType = "application/json";
        request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"lastName\":\"Smith\"}"));

        var (status, body) = Read(await function.PutAccounts(request, id));

        Assert.Equal(400, status);
        Assert.Contains("23503 ERROR", body);
    }

    private static (int Status, string Body) Read(IActionResult result) => result switch
    {
        ContentResult c => (c.StatusCode ?? 200, c.Content ?? ""),
        ObjectResult o => (o.StatusCode ?? 200, JsonSerializer.Serialize(o.Value, JsonDefaults.Options)),
        StatusCodeResult s => (s.StatusCode, ""),
        _ => throw new InvalidOperationException($"Unexpected result {result.GetType().Name}")
    };
}
