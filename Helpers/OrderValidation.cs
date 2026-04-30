using Models;

namespace Helpers
{
    public static class OrderValidation
    {
        public static string? ValidateCreateRequest(CreateOrderRequest request)
        {
            if (request.account_id <= 0)
            {
                return "account_id must be greater than zero";
            }

            if (request.order_data.ValueKind == System.Text.Json.JsonValueKind.Undefined || request.order_data.ValueKind == System.Text.Json.JsonValueKind.Null)
            {
                return "order_data is required";
            }

            if (!request.order_data.TryGetProperty("items", out var items) || items.ValueKind != System.Text.Json.JsonValueKind.Array || items.GetArrayLength() == 0)
            {
                return "order_data.items must be a non-empty array";
            }

            if (!request.order_data.TryGetProperty("total", out var total) || total.ValueKind != System.Text.Json.JsonValueKind.Number)
            {
                return "order_data.total is required and must be numeric";
            }

            if (!request.order_data.TryGetProperty("status", out var status) || status.ValueKind != System.Text.Json.JsonValueKind.String)
            {
                return "order_data.status is required";
            }

            return null;
        }
    }
}