using System.Text.Json.Serialization;

namespace SalesforceAccountFunctions.Models
{
    public class AccountDto
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        [JsonPropertyName("website")]
        public string? Website { get; set; }

        [JsonPropertyName("billingCity")]
        public string? BillingCity { get; set; }
    }
}
