using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using NSubstitute;
using Xunit;

namespace BusTravelAccountsSaFunction.Tests;

/// R2 — 400 "{statusCode} ERROR": POST /accounts, PUT /accounts/{id} database insert or update fails.
public class R2Tests
{
    private readonly IAccountsRepository repository = Substitute.For<IAccountsRepository>();

    [Fact]
    public async Task R2_post_database_insert_fails_answers_400()
    {
        repository.CreateAsync(Arg.Any<CreateAccountRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<string>(new PostgresException("insert failed", "ERROR", "ERROR", "23505")));

        var service = new AccountsService(repository, NullLogger<AccountsService>.Instance);
        var function = new AccountsFunctions(service, NullLogger<AccountsFunctions>.Instance);
        var request = new DefaultHttpContext().Request;
        request.Method = "POST";
        request.ContentType = "application/json";
        request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"firstName\":\"Ana\"}"));

        var exception = await Assert.ThrowsAsync<DatabaseWriteException>(() => function.PostAccounts(request));
        var (status, body) = Read(ErrorResults.DatabaseWriteFailure(exception.SqlState));

        Assert.Equal(400, status);
        Assert.Contains("23505 ERROR", body);
    }

    [Fact]
    public async Task R2_put_database_update_fails_answers_400()
    {
        repository.UpdateAsync("42", Arg.Any<UpdateAccountRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<string>(new PostgresException("update failed", "ERROR", "ERROR", "23514")));

        var service = new AccountsService(repository, NullLogger<AccountsService>.Instance);
        var function = new AccountsFunctions(service, NullLogger<AccountsFunctions>.Instance);
        var request = new DefaultHttpContext().Request;
        request.Method = "PUT";
        request.ContentType = "application/json";
        request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"lastName\":\"Smith\"}"));

        var exception = await Assert.ThrowsAsync<DatabaseWriteException>(() => function.PutAccounts(request, "42"));
        var (status, body) = Read(ErrorResults.DatabaseWriteFailure(exception.SqlState));

        Assert.Equal(400, status);
        Assert.Contains("23514 ERROR", body);
    }

    private static (int Status, string Body) Read(IActionResult result) => result switch
    {
        ContentResult c => (c.StatusCode ?? 200, c.Content ?? ""),
        ObjectResult o => (o.StatusCode ?? 200, JsonSerializer.Serialize(o.Value, JsonDefaults.Options)),
        StatusCodeResult s => (s.StatusCode, ""),
        _ => throw new InvalidOperationException($"Unexpected result {result.GetType().Name}")
    };
}
