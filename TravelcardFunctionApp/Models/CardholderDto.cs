using System.Text.Json.Serialization;

namespace TravelcardFunctionApp.Models
{
    public class CardholderDto
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
}
