using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TravelcardFunction.Models
{
    public class TravelcardDto
    {
        public int Id { get; set; }
        public TravelcardType TravelcardType { get; set; }
        public DateTime TravelcardValidFrom { get; set; }
        public DateTime TravelcardValidTo { get; set; }
        public string? TravelcardName { get; set; }
        public string? TravelcardNumber { get; set; }
        public DateTime TravelcardRequestedDate { get; set; }
        public string TravelcardTransactionReference { get; set; } = string.Empty;
        public DateTime? TravelcardUsableTo { get; set; }
        public IEnumerable<CardholderDto> Cardholders { get; set; } = Array.Empty<CardholderDto>();
    }

    public class CardholderDto
    {
        public int Id { get; set; }
        public int TravelcardId { get; set; }
        public string CardholderTitle { get; set; } = string.Empty;
        public string CardholderForename { get; set; } = string.Empty;
        public string CardholderSurname { get; set; } = string.Empty;
        public CardholderType CardholderType { get; set; }
        public string CardholderPhotoName { get; set; } = string.Empty;
        public string? CardholderPhotoRRSKey { get; set; }
        public string? CardholderPhotoURL { get; set; }
        public string? CardholderPhotoKey { get; set; }
    }
}
