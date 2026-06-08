// GENERATED_BY_AI_TEST_ENGINE
using System;
using System.IO;
using LifeTimeCalculator.Services;
using Xunit;

namespace LifeTimeCalculator.Tests.Services;

public class SecretHelperTests
{
    [Fact]
    public void Get_WhenCacheContainsSecret_ReturnsCachedValue()
    {
        // Arrange
        var secretName = Guid.NewGuid().ToString("N");
        var envFallback = Guid.NewGuid().ToString("N");
        var expected = "env-secret-value";
        var originalOut = Console.Out;
        var writer = new StringWriter();
        Console.SetOut(writer);
        Environment.SetEnvironmentVariable(envFallback, expected);

        try
        {
            // Act
            var firstValue = SecretHelper.Get(secretName, envFallback);
            var secondValue = SecretHelper.Get(secretName, envFallback);

            // Assert
            Assert.Equal(expected, firstValue);
            Assert.Equal(expected, secondValue);
        }
        finally
        {
            Console.SetOut(originalOut);
            Environment.SetEnvironmentVariable(envFallback, null);
        }
    }

    [Fact]
    public void Get_WhenEnvironmentFallbackIsSet_ReturnsEnvironmentValue()
    {
        // Arrange
        var secretName = Guid.NewGuid().ToString("N");
        var envFallback = Guid.NewGuid().ToString("N");
        var expected = "env-secret-value";
        var originalOut = Console.Out;
        var writer = new StringWriter();
        Console.SetOut(writer);
        Environment.SetEnvironmentVariable(envFallback, expected);

        try
        {
            // Act
            var result = SecretHelper.Get(secretName, envFallback);

            // Assert
            Assert.Equal(expected, result);
        }
        finally
        {
            Console.SetOut(originalOut);
            Environment.SetEnvironmentVariable(envFallback, null);
        }
    }

    [Fact]
    public void Get_WhenEnvironmentFallbackIsMissing_ReturnsEmptyString()
    {
        // Arrange
        var secretName = Guid.NewGuid().ToString("N");
        var envFallback = Guid.NewGuid().ToString("N");
        var originalOut = Console.Out;
        var writer = new StringWriter();
        Console.SetOut(writer);
        Environment.SetEnvironmentVariable(envFallback, null);

        try
        {
            // Act
            var result = SecretHelper.Get(secretName, envFallback);

            // Assert
            Assert.Equal(string.Empty, result);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void Get_WhenSecretNameIsEmpty_StillReturnsEnvironmentFallbackOrEmpty()
    {
        // Arrange
        var secretName = string.Empty;
        var envFallback = Guid.NewGuid().ToString("N");
        var expected = "fallback-value";
        var originalOut = Console.Out;
        var writer = new StringWriter();
        Console.SetOut(writer);
        Environment.SetEnvironmentVariable(envFallback, expected);

        try
        {
            // Act
            var result = SecretHelper.Get(secretName, envFallback);

            // Assert
            Assert.Equal(expected, result);
        }
        finally
        {
            Console.SetOut(originalOut);
            Environment.SetEnvironmentVariable(envFallback, null);
        }
    }

    [Fact]
    public void Get_WhenEnvFallbackIsNull_ReturnsEmptyString()
    {
        // Arrange
        var secretName = Guid.NewGuid().ToString("N");
        string? envFallback = null;
        var originalOut = Console.Out;
        var writer = new StringWriter();
        Console.SetOut(writer);

        try
        {
            // Act
            var result = SecretHelper.Get(secretName, envFallback ?? string.Empty);

            // Assert
            Assert.Equal(string.Empty, result);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void Get_WhenSecretNameIsNullOrWhitespace_ReturnsEnvironmentFallbackOrEmpty()
    {
        // Arrange
        var secretName = "   ";
        var envFallback = Guid.NewGuid().ToString("N");
        var expected = "fallback-value";
        var originalOut = Console.Out;
        var writer = new StringWriter();
        Console.SetOut(writer);
        Environment.SetEnvironmentVariable(envFallback, expected);

        try
        {
            // Act
            var result = SecretHelper.Get(secretName, envFallback);

            // Assert
            Assert.Equal(expected, result);
        }
        finally
        {
            Console.SetOut(originalOut);
            Environment.SetEnvironmentVariable(envFallback, null);
        }
    }

    [Fact]
    public void Get_WhenFallbackEnvironmentVariableIsWhitespace_ReturnsEmptyString()
    {
        // Arrange
        var secretName = Guid.NewGuid().ToString("N");
        var envFallback = Guid.NewGuid().ToString("N");
        var originalOut = Console.Out;
        var writer = new StringWriter();
        Console.SetOut(writer);
        Environment.SetEnvironmentVariable(envFallback, "   ");

        try
        {
            // Act
            var result = SecretHelper.Get(secretName, envFallback);

            // Assert
            Assert.Equal("   ", result);
        }
        finally
        {
            Console.SetOut(originalOut);
            Environment.SetEnvironmentVariable(envFallback, null);
        }
    }

    [Fact]
    public void Get_WhenSecretNameAndFallbackAreEmpty_ReturnsEmptyString()
    {
        // Arrange
        var secretName = string.Empty;
        var envFallback = string.Empty;
        var originalOut = Console.Out;
        var writer = new StringWriter();
        Console.SetOut(writer);

        try
        {
            // Act
            var result = SecretHelper.Get(secretName, envFallback);

            // Assert
            Assert.Equal(string.Empty, result);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void Get_WhenSecretNameIsNull_ReturnsEnvironmentFallbackOrEmpty()
    {
        // Arrange
        string? secretName = null;
        var envFallback = Guid.NewGuid().ToString("N");
        var expected = "fallback-value";
        var originalOut = Console.Out;
        var writer = new StringWriter();
        Console.SetOut(writer);
        Environment.SetEnvironmentVariable(envFallback, expected);

        try
        {
            // Act
            var result = SecretHelper.Get(secretName ?? string.Empty, envFallback);

            // Assert
            Assert.Equal(expected, result);
        }
        finally
        {
            Console.SetOut(originalOut);
            Environment.SetEnvironmentVariable(envFallback, null);
        }
    }

    [Fact]
    public void Get_WhenEnvironmentVariableContainsSensitiveValue_ReturnsValueWithoutThrowing()
    {
        // Arrange
        var secretName = Guid.NewGuid().ToString("N");
        var envFallback = Guid.NewGuid().ToString("N");
        var expected = "pci-token-12345";
        var originalOut = Console.Out;
        var writer = new StringWriter();
        Console.SetOut(writer);
        Environment.SetEnvironmentVariable(envFallback, expected);

        try
        {
            // Act
            var result = SecretHelper.Get(secretName, envFallback);

            // Assert
            Assert.Equal(expected, result);
        }
        finally
        {
            Console.SetOut(originalOut);
            Environment.SetEnvironmentVariable(envFallback, null);
        }
    }
}