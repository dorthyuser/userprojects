using System.Text.Json.Serialization;

namespace PrimeNumberService.Models;

public sealed record PrimeNumberResponse
{
    [JsonPropertyName("prime_number")]
    public int PrimeNumber { get; init; }
}