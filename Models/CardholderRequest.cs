namespace azurecsharppost1112.Models;

public sealed class CardholderRequest
{
    public string cardholderTitle { get; set; } = string.Empty;
    public string cardholderForename { get; set; } = string.Empty;
    public string cardholderSurname { get; set; } = string.Empty;
    public cardholderType_enum cardholderType { get; set; }
    public string cardholderPhotoName { get; set; } = string.Empty;
    public string? cardholderPhotoRRSKey { get; set; }
    public string? cardholderPhotoURL { get; set; }
    public string? cardholderPhotoKey { get; set; }
}
