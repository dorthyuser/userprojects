using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Helpers;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Models;

namespace Functions
{
    public class OrdersFunction
    {
        private readonly DbHelper _dbHelper;
        private readonly ILogger<OrdersFunction> _logger;

        public OrdersFunction(DbHelper dbHelper, ILogger<OrdersFunction> logger)
        {
            _dbHelper = dbHelper;
            _logger = logger;
        }

        [Function("GetOrders")]
        public async Task<HttpResponseData> GetOrders([HttpTrigger(AuthorizationLevel.Function, "get", Route = "orders")] HttpRequestData req)
        {
            _logger.LogInformation("Enter GetOrders");
            try
            {
                var orders = await _dbHelper.GetOrdersAsync();
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new { data = orders });
                _logger.LogInformation("Exit GetOrders");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetOrders");
                return await CreateErrorResponseAsync(req, HttpStatusCode.InternalServerError, "Failed to fetch orders", ex.Message);
            }
        }

        [Function("CreateOrder")]
        public async Task<HttpResponseData> CreateOrder([HttpTrigger(AuthorizationLevel.Function, "post", Route = "orders")] HttpRequestData req)
        {
            _logger.LogInformation("Enter CreateOrder");
            try
            {
                var body = await new System.IO.StreamReader(req.Body).ReadToEndAsync();
                if (string.IsNullOrWhiteSpace(body))
                {
                    return await CreateErrorResponseAsync(req, HttpStatusCode.BadRequest, "Request body is required", "Empty body");
                }

                var request = JsonSerializer.Deserialize<CreateOrderRequest>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (request == null)
                {
                    return await CreateErrorResponseAsync(req, HttpStatusCode.BadRequest, "Invalid JSON payload", "Deserialization failed");
                }

                var validationError = OrderValidation.ValidateCreateRequest(request);
                if (!string.IsNullOrEmpty(validationError))
                {
                    return await CreateErrorResponseAsync(req, HttpStatusCode.BadRequest, "Validation failed", validationError);
                }

                var created = await _dbHelper.CreateOrderAsync(request);
                var response = req.CreateResponse(HttpStatusCode.Created);
                await response.WriteAsJsonAsync(new { data = created });
                _logger.LogInformation("Exit CreateOrder");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateOrder");
                return await CreateErrorResponseAsync(req, HttpStatusCode.InternalServerError, "Failed to create order", ex.Message);
            }
        }

        [Function("DeleteOrder")]
        public async Task<HttpResponseData> DeleteOrder([HttpTrigger(AuthorizationLevel.Function, "delete", Route = "orders/{id:int}")] HttpRequestData req, int id)
        {
            _logger.LogInformation("Enter DeleteOrder for OrderId {OrderId}", id);
            try
            {
                if (id <= 0)
                {
                    return await CreateErrorResponseAsync(req, HttpStatusCode.BadRequest, "Invalid order id", "Id must be greater than zero");
                }

                var deleted = await _dbHelper.DeleteOrderAsync(id);
                if (!deleted)
                {
                    return await CreateErrorResponseAsync(req, HttpStatusCode.NotFound, "Order not found", $"No order exists with id {id}");
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new { message = "Order deleted successfully", id });
                _logger.LogInformation("Exit DeleteOrder for OrderId {OrderId}", id);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteOrder for OrderId {OrderId}", id);
                return await CreateErrorResponseAsync(req, HttpStatusCode.InternalServerError, "Failed to delete order", ex.Message);
            }
        }

        private static async Task<HttpResponseData> CreateErrorResponseAsync(HttpRequestData req, HttpStatusCode statusCode, string error, string details)
        {
            var response = req.CreateResponse(statusCode);
            await response.WriteAsJsonAsync(new { error, details });
            return response;
        }
    }
}