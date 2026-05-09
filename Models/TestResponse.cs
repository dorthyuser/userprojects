using System.Text.Json.Serialization;

namespace testing2.Models;

public sealed class TestResponse
{
    [JsonPropertyName("output")]
    public object Output { get; set; } = new();
}