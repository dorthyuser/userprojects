using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ResponseHttp;
using ResponseHttp.Models;
using ResponseHttp.Services;
using Xunit;

namespace ResponseHttp.Tests
{
    public class ResponseControllerTests
    {
        private class StubResponseService : IResponseService
        {
            private readonly Func<Task<string>> _func;
            public StubResponseService(Func<Task<string>> func) => _func = func;
            public Task<string> FetchAndFormatResponseAsync() => _func();
        }

        [Fact]
        public async Task Get_ReturnsOk_WithFetchResponseData()
        {
            // Arrange
            var expected = "payload";
            var svc = new StubResponseService(() => Task.FromResult(expected));
            var logger = NullLogger<ResponseController>.Instance;
            var controller = new ResponseController(svc, logger);

            // Act
            var result = await controller.Get();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var model = Assert.IsType<FetchResponse>(ok.Value);
            Assert.Equal(expected, model.Data);
        }

        [Fact]
        public async Task Get_WhenServiceThrows_Returns500()
        {
            // Arrange
            var svc = new StubResponseService(() => throw new InvalidOperationException("fail"));
            var logger = NullLogger<ResponseController>.Instance;
            var controller = new ResponseController(svc, logger);

            // Act
            var result = await controller.Get();

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
            Assert.IsType<string>(objectResult.Value);
        }
    }
}
