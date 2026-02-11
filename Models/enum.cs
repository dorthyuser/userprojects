using System.Text.Json.Serialization;

namespace SalesforceAccountFunctions.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SalesforceObjectType
    {
        Account
    }
}
