using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Azfcsharp8Function;

public static class AppHelpers
{
    public static string GetHeader(IHeaderDictionary headers, string name)
        => headers.TryGetValue(name, out var value) ? value.ToString() : string.Empty;

    public static string GetCorrelationId(IHeaderDictionary headers)
    {
        var value = GetHeader(headers, "X-Correlation-ID");
        return string.IsNullOrWhiteSpace(value) ? Guid.NewGuid().ToString() : value;
    }

    public static string TrimOrNull(string? value) => value is null ? string.Empty : value.Trim();

    public static string? TrimOrNullIfPresent(string? value) => value is null ? null : value.Trim();

    public static bool IsValidUuid(string value) => Guid.TryParse(value, out _);

    public static JsonSerializerOptions JsonOptions { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
}
