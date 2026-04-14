using System.Text.Json.Serialization;

namespace tc_csharp_api
{
    /// <summary>
    /// Placeholder model for travelcard requests. The application forwards raw request bodies unchanged,
    /// so this model is intentionally minimal and not required for the forwarding logic.
    /// </summary>
    public class TravelcardRequest
    {
        [JsonPropertyName("raw")]
        public string Raw { get; set; } = string.Empty;
    }
}
