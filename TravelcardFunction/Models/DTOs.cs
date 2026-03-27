using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TravelcardFunction.Models
{
    public class TravelcardRequest
    {
        public TravelcardType TravelcardType { get; set; }
        public DateTime TravelcardValidFrom { get; set; }
        public DateTime TravelcardValidTo { get; set; }
        public string TravelcardName { get; set; } = string.Empty;
        public string TravelcardNumber { get; set; } = string.Empty;
        public DateTime TravelcardRequestedDate { get; set; }
        public string TravelcardTransactionReference { get; set; } = string.Empty;
        public DateTime? TravelcardUsableTo { get; set; }
        public List<CardholderRequest> Cardholders { get; set; } = new List<CardholderRequest>();
    }

    public class CardholderRequest
    {
        public string CardholderTitle { get; set; } = string.Empty;
        public string CardholderForename { get; set; } = string.Empty;
        public string CardholderSurname { get; set; } = string.Empty;
        public CardholderType CardholderType { get; set; }
        public string CardholderPhotoName { get; set; } = string.Empty;
        public string CardholderPhotoRrsKey { get; set; } = string.Empty;
        public string CardholderPhotoURL { get; set; } = string.Empty;
        public string CardholderPhotoKey { get; set; } = string.Empty;
    }

    public class CreateTravelcardResponse
    {
        public string TravelcardId { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
    }

    public class ErrorResponse
    {
        public string Error { get; set; } = string.Empty;
        public IEnumerable<string>? Details { get; set; }
    }
}
