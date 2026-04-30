using System.Text.Json;

namespace Models
{
    public class CreateOrderRequest
    {
        public int account_id { get; set; }
        public JsonElement order_data { get; set; }
    }
}