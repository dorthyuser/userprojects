using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TravelcardFunction.Models
{
    public class TravelcardCreateRequest
    {
        [JsonPropertyName("travelcardType")]
        public TravelcardType TravelcardType { get; set; }

        [JsonPropertyName("travelcardValidFrom")]
        public DateTime TravelcardValidFrom { get; set; }

        [JsonPropertyName("travelcardValidTo")]
        public DateTime TravelcardValidTo { get; set; }

        [JsonPropertyName("travelcardName")]
        public string? TravelcardName { get; set; }

        [JsonPropertyName("travelcardNumber")]
        public string TravelcardNumber { get; set; } = string.Empty;

        [JsonPropertyName("travelcardRequestedDate")]
        public DateTime TravelcardRequestedDate { get; set; }

        [JsonPropertyName("travelcardTransactionReference")]
        public string TravelcardTransactionReference { get; set; } = string.Empty;

        [JsonPropertyName("travelcardUsableTo")]
        public DateTime? TravelcardUsableTo { get; set; }

        [JsonPropertyName("cardholders")]
        public List<CardholderCreateRequest> Cardholders { get; set; } = new List<CardholderCreateRequest>();
    }

    public class CardholderCreateRequest
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
        public string? CardholderPhotoRrsKey { get; set; }

        [JsonPropertyName("cardholderPhotoURL")]
        public string? CardholderPhotoUrl { get; set; }

        [JsonPropertyName("cardholderPhotoKey")]
        public string? CardholderPhotoKey { get; set; }
    }

    public class ErrorResponse
    {
        [JsonPropertyName("error")]
        public string Error { get; set; } = string.Empty;
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }

    // Entities used for DB
    public class TravelcardEntity
    {
        public TravelcardType TravelcardType { get; set; }
        public DateTime TravelcardValidFrom { get; set; }
        public DateTime TravelcardValidTo { get; set; }
        public string? TravelcardName { get; set; }
        public string? TravelcardNumber { get; set; }
        public DateTime TravelcardRequestedDate { get; set; }
        public string TravelcardTransactionReference { get; set; } = string.Empty;
        public DateTime? TravelcardUsableTo { get; set; }
    }

    public static class JsonSerializerOptionsProvider
    {
        public static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };
    }
}
