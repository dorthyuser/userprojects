using System;
using System.Collections.Generic;

namespace TravelcardFunctionApp.Models
{
    public class TravelcardRequest
    {
        public TravelcardType TravelcardType { get; set; } = TravelcardType.Young;
        public DateTime TravelcardValidFrom { get; set; } = DateTime.UtcNow;
        public DateTime TravelcardValidTo { get; set; } = DateTime.UtcNow.AddMonths(1);
        public string TravelcardName { get; set; } = string.Empty;
        public string TravelcardNumber { get; set; } = string.Empty;
        public DateTime TravelcardRequestedDate { get; set; } = DateTime.UtcNow;
        public string TravelcardTransactionReference { get; set; } = string.Empty;
        public DateTime? TravelcardUsableTo { get; set; }
        public List<CardholderRequest> Cardholders { get; set; } = new List<CardholderRequest>();
    }

    public class CardholderRequest
    {
        public string CardholderTitle { get; set; } = string.Empty;
        public string CardholderForename { get; set; } = string.Empty;
        public string CardholderSurname { get; set; } = string.Empty;
        public CardholderType CardholderType { get; set; } = CardholderType.Primary;
        public string CardholderPhotoName { get; set; } = string.Empty;
        public string CardholderPhotoRRSKey { get; set; } = string.Empty;
        public string CardholderPhotoURL { get; set; } = string.Empty;
        public string CardholderPhotoKey { get; set; } = string.Empty;
    }

    public class ErrorResponse
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
    }
}
