using System.Text.Json.Serialization;
namespace BusTravelAccountsSaMainLambda;

public sealed class CreatedAccountResponse
{
    public CreatedAccountResponse(string id)
    {
        Id = id;
    }

    [JsonPropertyName("id")]
    public string Id { get; set; }
}