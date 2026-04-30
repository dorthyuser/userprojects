using System;
using System.Text.Json;

namespace Models
{
    public class OrderItem
    {
        public string product { get; set; } = string.Empty;
        public int qty { get; set; }
        public decimal price { get; set; }
    }

    public class OrderData
    {
        public OrderItem[] items { get; set; } = Array.Empty<OrderItem>();
        public decimal total { get; set; }
        public string status { get; set; } = string.Empty;
    }

    public class AccountInfo
    {
        public int id { get; set; }
        public string name { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string? address { get; set; }
    }

    public class OrderResponse
    {
        public int id { get; set; }
        public int account_id { get; set; }
        public JsonElement order_data { get; set; }
        public DateTime created_at { get; set; }
        public AccountInfo account { get; set; } = new AccountInfo();
    }
}