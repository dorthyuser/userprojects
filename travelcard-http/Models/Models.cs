using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace travelcard_http.Models
{
    public class TravelcardRequest
    {
        public TravelcardType TravelcardType { get; set; }
        public DateTime TravelcardValidFrom { get; set; }
        public DateTime TravelcardValidTo { get; set; }
        public string? TravelcardName { get; set; }
        public string? TravelcardNumber { get; set; }
        public DateTime TravelcardRequestedDate { get; set; }
        public string? TravelcardTransactionReference { get; set; }
        public DateTime? TravelcardUsableTo { get; set; }
        public List<CardholderRequest>? Cardholders { get; set; }
    }

    public class CardholderRequest
    {
        public string? CardholderTitle { get; set; }
        public string? CardholderForename { get; set; }
        public string? CardholderSurname { get; set; }
        public CardholderType CardholderType { get; set; }
        public string? CardholderPhotoName { get; set; }
        public string? CardholderPhotoRrsKey { get; set; }
        public string? CardholderPhotoUrl { get; set; }
        public string? CardholderPhotoKey { get; set; }
    }

    public class TravelcardResponse
    {
        public string TravelcardId { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
    }

    public class ErrorResponse
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
