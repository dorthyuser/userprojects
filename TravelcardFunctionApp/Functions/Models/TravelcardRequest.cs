using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TravelcardFunctionApp.Models
{
    public class TravelcardRequest
    {
        [JsonPropertyName("travelcardType")]
        public TravelcardType TravelcardType { get; set; }

        [JsonPropertyName("travelcardValidFrom")]
        public DateTime TravelcardValidFrom { get; set; }

        [JsonPropertyName("travelcardValidTo")]
        public DateTime TravelcardValidTo { get; set; }

        [JsonPropertyName("travelcardName")]
        public string TravelcardName { get; set; } = string.Empty;

        [JsonPropertyName("travelcardNumber")]
        public string TravelcardNumber { get; set; } = string.Empty;

        [JsonPropertyName("travelcardRequestedDate")]
        public DateTime TravelcardRequestedDate { get; set; }

        [JsonPropertyName("travelcardTransactionReference")]
        public string TravelcardTransactionReference { get; set; } = string.Empty;

        [JsonPropertyName("travelcardUsableTo")]
        public DateTime? TravelcardUsableTo { get; set; }

        [JsonPropertyName("cardholders")]
        public List<CardholderRequest> Cardholders { get; set; } = new List<CardholderRequest>();
    }

    public class CardholderRequest
    {
        [JsonPropertyName("cardholderTitle")]
        public string CardholderTitle { get; set; } = string.Empty;

        [JsonPropertyName("cardholderForename")]
        public string CardholderForename { get; set; } = string.Empty;

        [JsonPropertyName("cardholderSurname")]
        public string CardholderSurname { get; set; } = string.Empty;

        [JsonPropertyName("cardholderType")]
        public CardholderType CardholderType { get; set; }

        [JsonPropertyName("cardholderPhotoName")]
        public string CardholderPhotoName { get; set; } = string.Empty;

        [JsonPropertyName("cardholderPhotoRRSKey")]
        public string? CardholderPhotoRRSKey { get; set; }

        [JsonPropertyName("cardholderPhotoURL")]
        public string? CardholderPhotoURL { get; set; }

        [JsonPropertyName("cardholderPhotoKey")]
        public string? CardholderPhotoKey { get; set; }
    }

    public class CreateTravelcardResponse
    {
        [JsonPropertyName("travelcardId")]
        public string TravelcardId { get; set; } = string.Empty;

        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;
    }

    public class ErrorResponse
    {
        [JsonPropertyName("error")]
        public string Error { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }
}
