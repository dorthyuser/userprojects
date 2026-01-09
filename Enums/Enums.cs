using System.Text.Json.Serialization;

namespace AccountsFunction.Enums
{
 [JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
 public enum SortOrder
 {
 Asc,
 Desc
 }
}
