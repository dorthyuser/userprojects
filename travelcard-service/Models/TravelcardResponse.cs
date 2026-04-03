using System;

namespace travelcard_service.Models
{
    public class TravelcardResponse
    {
        public string? Status { get; set; }
        public string? ExternalId { get; set; }
        public object? Raw { get; set; }
    }
}
