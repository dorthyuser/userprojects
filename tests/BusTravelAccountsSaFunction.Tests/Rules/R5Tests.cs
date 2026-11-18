using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace BusTravelAccountsSaFunction.Tests;

/// R5 — 405 "METHOD NOT ALLOWED": every endpoint known path with unsupported method.
public class R5Tests
{
    [Fact]
    public void R5_accounts_known_path_with_unsupported_method_answers_405()
    {
        var function = new FallbackFunctions();
        var request = new DefaultHttpContext().Request;
        request.Method = "DELETE";
        request.Path = "/accounts";

        var (status, body) = Read(function.AccountsMethodNotAllowed(request));

        Assert.Equal(405, status);
        Assert.Contains("METHOD NOT ALLOWED", body);
    }

    [Fact]
    public void R5_account_by_id_known_path_with_unsupported_method_answers_405()
    {
        var function = new FallbackFunctions();
        var request = new DefaultHttpContext().Request;
        request.Method = "POST";
        request.Path = "/accounts/42";

        var (status, body) = Read(function.AccountByIdMethodNotAllowed(request));

        Assert.Equal(405, status);
        Assert.Contains("METHOD NOT ALLOWED", body);
    }

    private static (int Status, string Body) Read(IActionResult result) => result switch
    {
        ContentResult c => (c.StatusCode ?? 200, c.Content ?? ""),
        ObjectResult o => (o.StatusCode ?? 200, JsonSerializer.Serialize(o.Value, JsonDefaults.Options)),
        StatusCodeResult s => (s.StatusCode, ""),
        _ => throw new InvalidOperationException($"Unexpected result {result.GetType().Name}")
    };
}
