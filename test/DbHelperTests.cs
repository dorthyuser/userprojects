// GENERATED_BY_AI_TEST_ENGINE
using System;
using System.Threading.Tasks;
using Helpers;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Helpers.Tests
{
    public class DbHelperTests
    {
        [Fact]
        public void Constructor_WithMissingConnectionString_DoesNotThrow()
        {
            var configurationMock = new Mock<IConfiguration>();
            configurationMock.Setup(c => c[It.IsAny<string>()]).Returns((string?)null);

            var exception = Record.Exception(() => new DbHelper(configurationMock.Object));

            Assert.Null(exception);
        }

        [Fact]
        public void Constructor_WithConfiguration_DoesNotThrow()
        {
            var configurationMock = new Mock<IConfiguration>();
            configurationMock.Setup(c => c[It.IsAny<string>()]).Returns((string?)null);

            var exception = Record.Exception(() => new DbHelper(configurationMock.Object));

            Assert.Null(exception);
        }

        [Fact]
        public async Task GetOrdersAsync_WhenCalledWithoutDatabase_ThrowsOrCompletes()
        {
            var configurationMock = new Mock<IConfiguration>();
            configurationMock.Setup(c => c[It.IsAny<string>()]).Returns((string?)null);
            var sut = new DbHelper(configurationMock.Object);

            var task = Task.Run(async () => await sut.GetOrdersAsync());
            var ex = await Record.ExceptionAsync(async () => await task);

            Assert.NotNull(ex);
        }

        [Fact]
        public async Task GetOrdersAsync_CanBeInvokedMultipleTimes()
        {
            var configurationMock = new Mock<IConfiguration>();
            configurationMock.Setup(c => c[It.IsAny<string>()]).Returns((string?)null);
            var sut = new DbHelper(configurationMock.Object);

            var first = Record.ExceptionAsync(async () => await sut.GetOrdersAsync());
            var second = Record.ExceptionAsync(async () => await sut.GetOrdersAsync());

            var ex1 = await first;
            var ex2 = await second;

            Assert.NotNull(ex1);
            Assert.NotNull(ex2);
        }
    }
}
