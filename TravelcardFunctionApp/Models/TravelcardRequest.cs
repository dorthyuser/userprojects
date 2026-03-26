using System;
using System.Collections.Generic;

namespace TravelcardFunctionApp.Models
{
    public class TravelcardRequest
    {
        public TravelcardType? TravelcardType { get; set; }
        public DateTime? TravelcardValidFrom { get; set; }
        public DateTime? TravelcardValidTo { get; set; }
        public string? TravelcardName { get; set; }
        public string? TravelcardNumber { get; set; }
        public DateTime? TravelcardRequestedDate { get; set; }
        public string? TravelcardTransactionReference { get; set; }
        public DateTime? TravelcardUsableTo { get; set; }
        public List<CardholderDto> Cardholders { get; set; } = new List<CardholderDto>();
    }
}
