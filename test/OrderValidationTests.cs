// GENERATED_BY_AI_TEST_ENGINE
using System.Text.Json;
using Helpers;
using Models;
using Xunit;

namespace Tests
{
    public class OrderValidationTests
    {
        [Fact]
        public void ValidateCreateRequest_ReturnsNull_WhenRequestIsValid()
        {
            var request = new CreateOrderRequest
            {
                account_id = 10,
                order_data = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(new
                {
                    items = new[] { 1 },
                    total = 25.5,
                    status = "open"
                }))
            };

            var result = OrderValidation.ValidateCreateRequest(request);

            Assert.Null(result);
        }

        [Fact]
        public void ValidateCreateRequest_ReturnsError_WhenAccountIdIsInvalid()
        {
            var request = new CreateOrderRequest
            {
                account_id = 0,
                order_data = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(new
                {
                    items = new[] { 1 },
                    total = 25.5,
                    status = "open"
                }))
            };

            var result = OrderValidation.ValidateCreateRequest(request);

            Assert.Equal("account_id must be greater than zero", result);
        }

        [Fact]
        public void ValidateCreateRequest_ReturnsError_WhenItemsAreMissing()
        {
            var request = new CreateOrderRequest
            {
                account_id = 1,
                order_data = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(new
                {
                    total = 25.5,
                    status = "open"
                }))
            };

            var result = OrderValidation.ValidateCreateRequest(request);

            Assert.Equal("order_data.items must be a non-empty array", result);
        }

        [Fact]
        public void ValidateCreateRequest_ReturnsError_WhenTotalIsMissing()
        {
            var request = new CreateOrderRequest
            {
                account_id = 1,
                order_data = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(new
                {
                    items = new[] { 1 },
                    status = "open"
                }))
            };

            var result = OrderValidation.ValidateCreateRequest(request);

            Assert.Equal("order_data.total is required and must be numeric", result);
        }
    }
}