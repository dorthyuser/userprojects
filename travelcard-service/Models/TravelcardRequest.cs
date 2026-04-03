using System;
using System.Text.Json.Serialization;

namespace travelcard_service.Models
{
    public class TravelcardRequest
    {
        public string? CardNumber { get; set; }
        public string? HolderName { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public TravelPurpose Purpose { get; set; } = TravelPurpose.Business;
    }
}
