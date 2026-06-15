using System.Text.Json.Serialization;

namespace travelcard_function_app.Models;

public class Cardholder
{
    public string CardholderTitle { get; set; } = string.Empty;
    public string CardholderForename { get; set; } = string.Empty;
    public string CardholderSurname { get; set; } = string.Empty;
    public CardholderType CardholderType { get; set; }
    public string CardholderPhotoName { get; set; } = string.Empty;
    public string? CardholderPhotoRRSKey { get; set; }
    public string? CardholderPhotoURL { get; set; }
    public string? CardholderPhotoKey { get; set; }
}
