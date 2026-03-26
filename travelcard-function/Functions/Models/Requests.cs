using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TravelcardFunction.Models
{
    public class CardholderDto
    {
        public string CardholderTitle { get; set; } = string.Empty;
        public string CardholderForename { get; set; } = string.Empty;
        public string CardholderSurname { get; set; } = string.Empty;
        public CardholderType CardholderType { get; set; }
        public string CardholderPhotoName { get; set; } = string.Empty;
        public string? CardholderPhotoRrsKey { get; set; }
        public string? CardholderPhotoUrl { get; set; }
        public string? CardholderPhotoKey { get; set; }
    }

    public class TravelcardCreateRequest
    {
        public TravelcardType TravelcardType { get; set; }
        public DateTime TravelcardValidFrom { get; set; }
        public DateTime TravelcardValidTo { get; set; }
        public string? TravelcardName { get; set; }
        public string TravelcardNumber { get; set; } = string.Empty;
        public DateTime TravelcardRequestedDate { get; set; }
        public string TravelcardTransactionReference { get; set; } = string.Empty;
        public DateTime? TravelcardUsableTo { get; set; }
        public List<CardholderDto> Cardholders { get; set; } = new List<CardholderDto>();
    }
}
