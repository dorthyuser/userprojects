using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using hello_http_test.Controllers;
using hello_http_test.Models;
using Microsoft.Extensions.Logging;

namespace hello_http_test.Tests
{
    public class StoresControllerTests
    {
        private readonly NullLogger<StoresController> _logger = new();

        [Fact]
        public async Task GetStores_ReturnsOk_WhenServiceSucceeds()
        {
            var fake = new FakeZohoStoreService { OnGet = () => Task.FromResult<string>("[]") };
            var controller = new StoresController(fake, _logger);

            var result = await controller.GetStores();
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("[]", ok.Value);
        }

        [Fact]
        public async Task GetStores_Returns500_WhenServiceThrows()
        {
            var fake = new FakeZohoStoreService { OnGet = () => throw new InvalidOperationException("boom") };
            var controller = new StoresController(fake, _logger);

            var result = await controller.GetStores();
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
            Assert.NotNull(objectResult.Value);
        }

        [Fact]
        public async Task CreateStore_Null_ReturnsBadRequest()
        {
            var fake = new FakeZohoStoreService();
            var controller = new StoresController(fake, _logger);

            var result = await controller.CreateStore(null);
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task CreateStore_ReturnsCreated_WhenServiceSucceeds()
        {
            var created = "{"id":"1"}";
            var fake = new FakeZohoStoreService { OnCreate = s => Task.FromResult(created) };
            var controller = new StoresController(fake, _logger);

            var result = await controller.CreateStore(new Store { Id = "1" });
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(created, createdResult.Value);
        }

        [Fact]
        public async Task CreateStore_Returns500_WhenServiceThrows()
        {
            var fake = new FakeZohoStoreService { OnCreate = s => throw new Exception("fail") };
            var controller = new StoresController(fake, _logger);

            var result = await controller.CreateStore(new Store { Id = "1" });
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
        }

        [Fact]
        public async Task UpdateStore_BadRequest_WhenInvalid()
        {
            var fake = new FakeZohoStoreService();
            var controller = new StoresController(fake, _logger);

            var r1 = await controller.UpdateStore(null, new Store());
            Assert.IsType<BadRequestResult>(r1);

            var r2 = await controller.UpdateStore("id", null);
            Assert.IsType<BadRequestResult>(r2);
        }

        [Fact]
        public async Task UpdateStore_ReturnsOk_WhenServiceSucceeds()
        {
            var fake = new FakeZohoStoreService { OnUpdate = (id, s) => Task.FromResult("ok") };
            var controller = new StoresController(fake, _logger);

            var result = await controller.UpdateStore("1", new Store { Id = "1" });
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("ok", ok.Value);
        }

        [Fact]
        public async Task UpdateStore_Returns500_WhenServiceThrows()
        {
            var fake = new FakeZohoStoreService { OnUpdate = (id, s) => throw new Exception("x") };
            var controller = new StoresController(fake, _logger);

            var result = await controller.UpdateStore("1", new Store { Id = "1" });
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
        }
    }
}
