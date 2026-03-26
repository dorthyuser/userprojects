using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TravelcardApi.Models
{
    public class CardholderDto
    {
        public string cardholderTitle { get; set; } = string.Empty;
        public string cardholderForename { get; set; } = string.Empty;
        public string cardholderSurname { get; set; } = string.Empty;
        public CardholderType cardholderType { get; set; } = CardholderType.Primary;
        public string cardholderPhotoName { get; set; } = string.Empty;
        public string? cardholderPhotoRRSKey { get; set; }
        public string? cardholderPhotoURL { get; set; }
        public string? cardholderPhotoKey { get; set; }
    }

    public class TravelcardRequest
    {
        public TravelcardType travelcardType { get; set; } = TravelcardType.Young;
        public DateTime travelcardValidFrom { get; set; } = DateTime.UtcNow;
        public DateTime travelcardValidTo { get; set; } = DateTime.UtcNow.AddDays(1);
        public string? travelcardName { get; set; }
        public string travelcardNumber { get; set; } = string.Empty;
        public DateTime travelcardRequestedDate { get; set; } = DateTime.UtcNow;
        public string travelcardTransactionReference { get; set; } = string.Empty;
        public DateTime? travelcardUsableTo { get; set; }
        public List<CardholderDto> cardholders { get; set; } = new List<CardholderDto>();
    }

    public class TravelcardResponse
    {
        public string travelcardId { get; set; } = string.Empty;
        public string token { get; set; } = string.Empty;
    }

    public class ValidationException : Exception
    {
        public List<string> Errors { get; }
        public ValidationException(List<string> errors)
        {
            Errors = errors;
        }
    }
}
