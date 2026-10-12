using HotelBookingService.Infrastructure;
using System.Reflection;
using Xunit;

namespace HotelBookingService.Tests;

public sealed class SecretsHelperTests
{
    private static void ResetCache()
    {
        var field = typeof(SecretsHelper).GetField("_cache", BindingFlags.NonPublic | BindingFlags.Static);
        field!.SetValue(null, null);
    }

    [Fact]
    public void Get_ReturnsEnvironmentFallback_WhenSecretNameIsMissing()
    {
        ResetCache();
        Environment.SetEnvironmentVariable("SECRET_NAME", null);
        Environment.SetEnvironmentVariable("FALLBACK_KEY", "fallback-value");

        var result = SecretsHelper.Get("any", "FALLBACK_KEY");

        Assert.Equal("fallback-value", result);
    }

    [Fact]
    public void Get_ReturnsEmptyString_WhenEnvironmentFallbackIsMissing()
    {
        ResetCache();
        Environment.SetEnvironmentVariable("SECRET_NAME", null);
        Environment.SetEnvironmentVariable("MISSING_FALLBACK", null);

        var result = SecretsHelper.Get("any", "MISSING_FALLBACK");

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Get_ReturnsCachedSecretValue_WhenSecretExistsAndIsNotWhitespace()
    {
        ResetCache();
        Environment.SetEnvironmentVariable("SECRET_NAME", null);
        Environment.SetEnvironmentVariable("FALLBACK_KEY", "fallback-value");

        var cacheField = typeof(SecretsHelper).GetField("_cache", BindingFlags.NonPublic | BindingFlags.Static);
        cacheField!.SetValue(null, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["apiKey"] = "secret-value"
        });

        var result = SecretsHelper.Get("apiKey", "FALLBACK_KEY");

        Assert.Equal("secret-value", result);
    }

    [Fact]
    public void Get_ReturnsEnvironmentFallback_WhenCachedSecretValueIsWhitespace()
    {
        ResetCache();
        Environment.SetEnvironmentVariable("SECRET_NAME", null);
        Environment.SetEnvironmentVariable("FALLBACK_KEY", "fallback-value");

        var cacheField = typeof(SecretsHelper).GetField("_cache", BindingFlags.NonPublic | BindingFlags.Static);
        cacheField!.SetValue(null, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["apiKey"] = "   "
        });

        var result = SecretsHelper.Get("apiKey", "FALLBACK_KEY");

        Assert.Equal("fallback-value", result);
    }
}
