// GENERATED_BY_AI_TEST_ENGINE
using System;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using LifeTimeCalculatorLambda;
using LifeTimeCalculatorLambda.Models;
using Xunit;

namespace LifeTimeCalculatorLambda.Tests;

public class FunctionTests
{
    [Fact]
    public async Task life_time_calculator_ReturnsResponse_WhenServiceSucceeds()
    {
        var expected = new Response
        {
            dateOfBirth = "2000-01-01T00:00:00.0000000+00:00",
            currentDate = "2024-01-01T00:00:00.0000000+00:00",
            age = new Age(),
            lifetimeStats = new LifetimeStats()
        };

        var sut = new Function();
        var loggerMock = new TestLambdaLogger();
        var context = new TestLambdaContext(loggerMock);

        Response result = await sut.life_time_calculator(new Request(), context);

        Assert.NotNull(result);
        Assert.Equal(expected.dateOfBirth, result.dateOfBirth);
        Assert.Equal(expected.currentDate, result.currentDate);
    }

    [Fact]
    public async Task life_time_calculator_LogsAndRethrows_WhenServiceThrows()
    {
        var sut = new Function();
        var loggerMock = new TestLambdaLogger();
        var context = new TestLambdaContext(loggerMock);

        Task<Response> act = sut.life_time_calculator(new Request(), context);

        await Assert.ThrowsAsync<ArgumentException>(async () => await act);
        Assert.Contains("An error occurred while processing the request.", loggerMock.LoggedMessage);
    }

    private sealed class TestLambdaContext : ILambdaContext
    {
        public TestLambdaContext(ILambdaLogger logger)
        {
            Logger = logger;
        }

        public string AwsRequestId => "test-request-id";
        public IClientContext ClientContext => null!;
        public string FunctionName => "test-function";
        public string FunctionVersion => "1";
        public ICognitoIdentity Identity => null!;
        public string InvokedFunctionArn => "arn:aws:lambda:us-east-1:123456789012:function:test-function";
        public ILambdaLogger Logger { get; }
        public string LogGroupName => "test-log-group";
        public string LogStreamName => "test-log-stream";
        public int MemoryLimitInMB => 128;
        public TimeSpan RemainingTime => TimeSpan.FromMinutes(5);
    }

    private sealed class TestLambdaLogger : ILambdaLogger
    {
        public string LoggedMessage { get; private set; } = string.Empty;

        public void Log(string message)
        {
            LoggedMessage = message;
        }

        public void LogLine(string message)
        {
            LoggedMessage = message;
        }
    }
}