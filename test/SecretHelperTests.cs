// GENERATED_BY_AI_TEST_ENGINE
using System;
using Xunit;
using new_test_for_demo.Services;

namespace new_test_for_demo.Tests;

public class SecretHelperTests
{
    [Fact]
    public void Get_ReturnsEnvironmentFallback_WhenSecretIsNotInCacheAndKeyVaultUnavailable()
    {
        var envName = "TEST_SECRET_HELPER_FALLBACK";
        var expected = "fallback-value";
        Environment.SetEnvironmentVariable(envName, expected);

        var result = SecretHelper.Get("missing-secret-name", envName);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Get_ReturnsEmptyString_WhenEnvironmentFallbackIsMissing()
    {
        var envName = "TEST_SECRET_HELPER_MISSING";
        Environment.SetEnvironmentVariable(envName, null);

        var result = SecretHelper.Get("missing-secret-name-2", envName);

        Assert.Equal(string.Empty, result);
    }
}