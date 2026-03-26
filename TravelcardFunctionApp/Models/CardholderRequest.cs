using System;
using System.Text.RegularExpressions;
using System.Text.Json.Serialization;
using TravelcardFunctionApp.Enums;

namespace TravelcardFunctionApp.Models
{
    public class CardholderRequest
    {
        [JsonPropertyName("cardholderTitle")]
        public string CardholderTitle { get; set; } = string.Empty;

        [JsonPropertyName("cardholderForename")]
        public string CardholderForename { get; set; } = string.Empty;

        [JsonPropertyName("cardholderSurname")]
        public string CardholderSurname { get; set; } = string.Empty;

        [JsonPropertyName("cardholderType")]
        public CardholderType CardholderType { get; set; } = CardholderType.Primary;

        [JsonPropertyName("cardholderPhotoName")]
        public string CardholderPhotoName { get; set; } = string.Empty;

        [JsonPropertyName("cardholderPhotoRRSKey")]
        public string? CardholderPhotoRRSKey { get; set; }

        [JsonPropertyName("cardholderPhotoURL")]
        public string? CardholderPhotoURL { get; set; }

        [JsonPropertyName("cardholderPhotoKey")]
        public string? CardholderPhotoKey { get; set; }

        public (bool IsValid, string ErrorMessage) Validate()
        {
            if (string.IsNullOrWhiteSpace(CardholderTitle) || CardholderTitle.Length > 15) return (false, "cardholderTitle is required and must be <= 15 characters");
            if (string.IsNullOrWhiteSpace(CardholderForename) || CardholderForename.Length > 100) return (false, "cardholderForename is required and must be <= 100 characters");
            if (string.IsNullOrWhiteSpace(CardholderSurname) || CardholderSurname.Length > 100) return (false, "cardholderSurname is required and must be <= 100 characters");
            if (string.IsNullOrWhiteSpace(CardholderPhotoName) || CardholderPhotoName.Length > 100) return (false, "cardholderPhotoName is required and must be <= 100 characters");

            // Exactly one of the photo fields must be provided
            var provided = 0;
            if (!string.IsNullOrWhiteSpace(CardholderPhotoRRSKey)) provided++;
            if (!string.IsNullOrWhiteSpace(CardholderPhotoURL)) provided++;
            if (!string.IsNullOrWhiteSpace(CardholderPhotoKey)) provided++;
            if (provided != 1) return (false, "Exactly one of cardholderPhotoRRSKey, cardholderPhotoURL, cardholderPhotoKey must be provided");

            if (!string.IsNullOrWhiteSpace(CardholderPhotoRRSKey))
            {
                if (CardholderPhotoRRSKey.Length < 39 || CardholderPhotoRRSKey.Length > 42) return (false, "cardholderPhotoRRSKey length must be between 39 and 42");
            }

            if (!string.IsNullOrWhiteSpace(CardholderPhotoKey))
            {
                if (CardholderPhotoKey.Length < 39 || CardholderPhotoKey.Length > 42) return (false, "cardholderPhotoKey length must be between 39 and 42");
            }

            if (!string.IsNullOrWhiteSpace(CardholderPhotoURL))
            {
                if (CardholderPhotoURL.Length < 20 || CardholderPhotoURL.Length > 2048) return (false, "cardholderPhotoURL length must be between 20 and 2048");
                // Basic URI check
                if (!Uri.IsWellFormedUriString(CardholderPhotoURL, UriKind.Absolute)) return (false, "cardholderPhotoURL must be a valid URI");
            }

            // Basic name regex to match DB constraints - simplified
            var nameRegex = new Regex("^(?!.*[×÷ˇ˘μ])[A-Za-zÀ-ž .'’\\-]+$");
            if (!nameRegex.IsMatch(CardholderForename)) return (false, "cardholderForename contains invalid characters");
            if (!nameRegex.IsMatch(CardholderSurname)) return (false, "cardholderSurname contains invalid characters");

            return (true, string.Empty);
        }
    }
}
