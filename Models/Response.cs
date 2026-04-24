using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UpTransportTicketApiLambda.Models
{
    public class Response
    {
        public Response()
        {
            Categories = new List<Category>();
            Name = string.Empty;
        }

        public Guid Id { get; set; }
        public string Name { get; set; }
        public List<Category> Categories { get; set; }
        public decimal Price { get; set; }
        public decimal DiscountedPrice { get; set; }
        public bool Expired { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
