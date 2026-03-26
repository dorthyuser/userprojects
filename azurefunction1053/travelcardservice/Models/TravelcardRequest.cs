using System;

namespace travelcardservice.Models
{
    public class TravelcardRequest
    {
        public TravelcardType TravelcardType { get; set; } = TravelcardType.Young;
        public DateTime TravelcardValidFrom { get; set; } = DateTime.UtcNow;
        public DateTime TravelcardValidTo { get; set; } = DateTime.UtcNow.AddDays(1);
        public string TravelcardName { get; set; } = string.Empty;
        public string TravelcardNumber { get; set; } = string.Empty;
        public DateTime TravelcardRequestedDate { get; set; } = DateTime.UtcNow;
        public string TravelcardTransactionReference { get; set; } = string.Empty;
        public DateTime? TravelcardUsableTo { get; set; }
        public CardholderDto[] Cardholders { get; set; } = Array.Empty<CardholderDto>();
    }
}
