using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace BusTravelAccountsSaFunction.Tests;

/// R4 — 404 "RESOURCE NOT FOUND": every endpoint when APIKIT:NOT_FOUND.
public class R4Tests
{
    [Fact]
    public void R4_unknown_path_answers_404()
    {
        var function = new FallbackFunctions();
        var context = new DefaultHttpContext();
        var request = context.Request;
        request.Method = "GET";
        request.Path = "/missing";

        var (status, body) = Read(function.ResourceNotFound(request));

        Assert.Equal(404, status);
        Assert.Contains("RESOURCE NOT FOUND", body);
    }

    private static (int Status, string Body) Read(IActionResult result) => result switch
    {
        ContentResult c => (c.StatusCode ?? 200, c.Content ?? ""),
        ObjectResult o => (o.StatusCode ?? 200, JsonSerializer.Serialize(o.Value, JsonDefaults.Options)),
        StatusCodeResult s => (s.StatusCode, ""),
        _ => throw new InvalidOperationException($"Unexpected result {result.GetType().Name}")
    };
}
