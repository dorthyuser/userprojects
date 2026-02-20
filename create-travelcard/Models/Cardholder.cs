using System.Text.Json.Serialization;

namespace CreateTravelcard.Models
{
    public class Cardholder
    {
        [JsonPropertyName("cardholderTitle")]
        public string CardholderTitle { get; set; }

        [JsonPropertyName("cardholderForename")]
        public string CardholderForename { get; set; }

        [JsonPropertyName("cardholderSurname")]
        public string CardholderSurname { get; set; }

        [JsonPropertyName("cardholderType")]
        public CardholderType CardholderType { get; set; }

        [JsonPropertyName("cardholderPhotoName")]
        public string CardholderPhotoName { get; set; }

        [JsonPropertyName("cardholderPhotoRRSKey")]
        public string CardholderPhotoRRSKey { get; set; }

        [JsonPropertyName("cardholderPhotoURL")]
        public string CardholderPhotoURL { get; set; }

        [JsonPropertyName("cardholderPhotoKey")]
        public string CardholderPhotoKey { get; set; }
    }
}
