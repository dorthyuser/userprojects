using System;
using System.Collections.Generic;

namespace TravelcardFunction.Models
{
    public class TravelcardRequest
    {
        public TravelcardType TravelcardType { get; set; } = TravelcardType.Young;
        public DateTime TravelcardValidFrom { get; set; } = DateTime.UtcNow;
        public DateTime TravelcardValidTo { get; set; } = DateTime.UtcNow.AddMonths(1);
        public string? TravelcardName { get; set; } = null;
        public string TravelcardNumber { get; set; } = string.Empty;
        public DateTime TravelcardRequestedDate { get; set; } = DateTime.UtcNow;
        public string TravelcardTransactionReference { get; set; } = string.Empty;
        public DateTime? TravelcardUsableTo { get; set; } = null;
        public List<CardholderRequest> Cardholders { get; set; } = new List<CardholderRequest>();
    }

    public class CardholderRequest
    {
        public string CardholderTitle { get; set; } = string.Empty;
        public string CardholderForename { get; set; } = string.Empty;
        public string CardholderSurname { get; set; } = string.Empty;
        public CardholderType CardholderType { get; set; } = CardholderType.Primary;
        public string CardholderPhotoName { get; set; } = string.Empty;
        public string? CardholderPhotoRRSKey { get; set; } = null;
        public string? CardholderPhotoURL { get; set; } = null;
        public string? CardholderPhotoKey { get; set; } = null;
    }
}
