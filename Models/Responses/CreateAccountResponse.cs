using System.Text.Json.Serialization;
namespace BusTravelAccountsSaMainLambda;

public sealed class CreateAccountResponse
{
    public CreateAccountResponse(string id)
    {
        Id = id;
    }

    [JsonPropertyName("id")]
    public string Id { get; set; }
}
