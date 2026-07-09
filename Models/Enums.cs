using System.Text.Json.Serialization;

namespace currency_calculator.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CurrencyCodeEnum
{
    USD,
    INR,
    EUR,
    GBP,
    AED,
    CAD,
    AUD,
    SGD,
    JPY
}