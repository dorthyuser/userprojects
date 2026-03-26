using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text.Json.Serialization;
using TravelcardFunctionApp.Enums;

namespace TravelcardFunctionApp.Models
{
    public class TravelcardRequest
    {
        [JsonPropertyName("travelcardType")]
        public TravelcardType TravelcardType { get; set; } = TravelcardType.Young;

        [JsonPropertyName("travelcardValidFrom")]
        public DateTime TravelcardValidFrom { get; set; }

        [JsonPropertyName("travelcardValidTo")]
        public DateTime TravelcardValidTo { get; set; }

        [JsonPropertyName("travelcardName")]
        public string? TravelcardName { get; set; }

        [JsonPropertyName("travelcardNumber")]
        public string? TravelcardNumber { get; set; }

        [JsonPropertyName("travelcardRequestedDate")]
        public DateTime TravelcardRequestedDate { get; set; }

        [JsonPropertyName("travelcardTransactionReference")]
        public string? TravelcardTransactionReference { get; set; }

        [JsonPropertyName("travelcardUsableTo")]
        public DateTime? TravelcardUsableTo { get; set; }

        [JsonPropertyName("cardholders")]
        public List<CardholderRequest> Cardholders { get; set; } = new List<CardholderRequest>();

        public (bool IsValid, string ErrorMessage) Validate()
        {
            // travelcardNumber length 11-22
            if (string.IsNullOrWhiteSpace(TravelcardNumber) || TravelcardNumber.Length < 11 || TravelcardNumber.Length > 22)
            {
                return (false, "travelcardNumber must be 11-22 characters");
            }

            if (string.IsNullOrEmpty(TravelcardTransactionReference) || TravelcardTransactionReference.Length != 15)
            {
                return (false, "travelcardTransactionReference must be exactly 15 characters");
            }

            // valid_from/valid_to presence checked by types

            // cardholders 1 or 2 and exactly one primary
            if (Cardholders == null || Cardholders.Count < 1 || Cardholders.Count > 2)
            {
                return (false, "cardholders must contain 1 or 2 items");
            }

            if (!Cardholders.Any(c => c.CardholderType == CardholderType.Primary))
            {
                return (false, "At least one Primary cardholder is required");
            }

            foreach (var ch in Cardholders)
            {
                var chValid = ch.Validate();
                if (!chValid.IsValid) return chValid;
            }

            if (!string.IsNullOrEmpty(TravelcardName) && TravelcardName.Length > 255) return (false, "travelcardName must be <= 255 characters");

            return (true, string.Empty);
        }
    }
}
