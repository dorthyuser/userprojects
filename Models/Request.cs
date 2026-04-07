using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace TcLambdaLambda.Models
{
    public class Request
    {
        // The API must forward the raw body as-is. This model can be used for optional validation.
        public Request()
        {
            Body = string.Empty;
        }

        [JsonPropertyName("body")]
        public string Body { get; set; }

        // Example field showing enum usage (if client supplies type in JSON)
        public TravelcardType? TravelcardType { get; set; }

        // Add validation helper
        public System.Collections.Generic.List<string> Validate()
        {
            var errors = new System.Collections.Generic.List<string>();
            // If TravelcardType is supplied, ensure it is a defined enum value (System.Text.Json handles this)
            // Additional regex or pattern validations can be added here.
            return errors;
        }
    }
}
