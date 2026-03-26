using System;
using System.Text.Json.Serialization;

namespace travelcard_function.Models
{
    public class CreateTravelcardRequest
    {
        public TravelcardType travelcardType { get; set; } = TravelcardType.Young;
        public DateTime travelcardValidFrom { get; set; } = DateTime.UtcNow;
        public DateTime travelcardValidTo { get; set; } = DateTime.UtcNow.AddMonths(1);
        public string travelcardName { get; set; } = string.Empty;
        public string travelcardNumber { get; set; } = string.Empty;
        public DateTime travelcardRequestedDate { get; set; } = DateTime.UtcNow;
        public string travelcardTransactionReference { get; set; } = string.Empty;
        public DateTime? travelcardUsableTo { get; set; }
        public CardholderDto[] cardholders { get; set; } = Array.Empty<CardholderDto>();
    }

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

    public class CreateTravelcardResponse
    {
        public string travelcardId { get; set; } = string.Empty;
        public string token { get; set; } = string.Empty;
    }

    public class ErrorResponse
    {
        public ErrorResponse() { }
        public ErrorResponse(string message) { this.error = message; }
        public string error { get; set; } = string.Empty;
    }
}
