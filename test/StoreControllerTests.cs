using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using hello_http_test.Controllers;
using hello_http_test.Models;
using Microsoft.Extensions.Logging;

namespace hello_http_test.Tests
{
    public class StoreControllerTests
    {
        private readonly NullLogger<StoreController> _logger = new();

        [Fact]
        public async Task GetStores_ReturnsOk_WithServiceResult()
        {
            var fake = new FakeStoreService { OnGet = () => Task.FromResult<object>(new { items = new[] { 1 } }) };
            var controller = new StoreController(fake, _logger);

            var result = await controller.GetStores();

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task CreateStore_NullPayload_ReturnsBadRequest()
        {
            var fake = new FakeStoreService();
            var controller = new StoreController(fake, _logger);

            var result = await controller.CreateStore(null);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Store payload required.", bad.Value);
        }

        [Fact]
        public async Task CreateStore_Valid_ReturnsCreated()
        {
            var createdObj = new { id = "123" };
            var fake = new FakeStoreService { OnCreate = store => Task.FromResult<object>(createdObj) };
            var controller = new StoreController(fake, _logger);

            var dto = new StoreDto { Id = "1", Name = "s" };
            var result = await controller.CreateStore(dto);

            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(createdObj, created.Value);
        }

        [Fact]
        public async Task UpdateStore_InvalidId_ReturnsBadRequest()
        {
            var fake = new FakeStoreService();
            var controller = new StoreController(fake, _logger);

            var result = await controller.UpdateStore("", new StoreDto());

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Id required.", bad.Value);
        }

        [Fact]
        public async Task UpdateStore_NullPayload_ReturnsBadRequest()
        {
            var fake = new FakeStoreService();
            var controller = new StoreController(fake, _logger);

            var result = await controller.UpdateStore("123", null);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Store payload required.", bad.Value);
        }

        [Fact]
        public async Task UpdateStore_Valid_ReturnsOk()
        {
            var expected = new { ok = true };
            var fake = new FakeStoreService { OnUpdate = (id, s) => Task.FromResult<object>(expected) };
            var controller = new StoreController(fake, _logger);

            var result = await controller.UpdateStore("1", new StoreDto { Id = "1" });

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, ok.Value);
        }
    }
}
