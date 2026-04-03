using System;
using System.Text.Json.Serialization;

namespace TravelcardGatewayService.Models
{
    public class TravelcardRequest
    {
        public string RequestId { get; set; } = Guid.NewGuid().ToString();
        public string FullName { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public DateTime TravelDate { get; set; } = DateTime.UtcNow;
        public int TravelersCount { get; set; } = 1;
    }
}
