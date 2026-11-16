using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace BusTravelAccountsSaFunction.Tests;

/// R2 — 400 "{statusCode} ERROR": POST /accounts, PUT /accounts/{id} when Salesforce create or update result is not successful.
public class R2Tests
{
    [Fact]
    public async Task R2_post_accounts_with_unsuccessful_salesforce_create_answers_400()
    {
        var repository = Substitute.For<IAccountsRepository>();
        repository.CreateAccountAsync(Arg.Any<JsonObject>(), Arg.Any<CancellationToken>())
            .Returns(new SalesforceBulkResult
            {
                Successful = false,
                Items =
                [
                    new SalesforceBulkItem
                    {
                        Successful = false,
                        Payload = new JsonObject
                        {
                            ["errors"] = new JsonArray
                            {
                                new JsonObject
                                {
                                    ["statusCode"] = "INVALID_FIELD",
                                    ["message"] = "Create failed"
                                }
                            }
                        }
                    }
                ]
            });
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
        request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"lastName\":\"Ana\"}"));

        var (status, body) = Read(await function.PostAccounts(request));

        Assert.Equal(400, status);
        Assert.Contains("INVALID_FIELD ERROR", body);
    }

    [Fact]
    public async Task R2_put_accounts_with_unsuccessful_salesforce_update_answers_400()
    {
        var repository = Substitute.For<IAccountsRepository>();
        repository.UpdateAccountAsync(Arg.Any<JsonObject>(), Arg.Any<CancellationToken>())
            .Returns(new SalesforceBulkResult
            {
                Successful = false,
                Items =
                [
                    new SalesforceBulkItem
                    {
                        Successful = false,
                        Payload = new JsonObject
                        {
                            ["errors"] = new JsonArray
                            {
                                new JsonObject
                                {
                                    ["statusCode"] = "DUPLICATE_VALUE",
                                    ["message"] = "Update failed"
                                }
                            }
                        }
                    }
                ]
            });
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
        request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"lastName\":\"Ana\"}"));

        var (status, body) = Read(await function.PutAccounts(request, "42"));

        Assert.Equal(400, status);
        Assert.Contains("DUPLICATE_VALUE ERROR", body);
    }

    private static (int Status, string Body) Read(IActionResult result) => result switch
    {
        ContentResult c => (c.StatusCode ?? 200, c.Content ?? ""),
        ObjectResult o => (o.StatusCode ?? 200, JsonSerializer.Serialize(o.Value, JsonDefaults.Options)),
        StatusCodeResult s => (s.StatusCode, ""),
        _ => throw new InvalidOperationException($"Unexpected result {result.GetType().Name}")
    };
}
