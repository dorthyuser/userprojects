namespace azuresharpapi153.Models.Requests;

public sealed class CardholderRequest
{
    public string CardholderTitle { get; set; } = string.Empty;
    public string CardholderForename { get; set; } = string.Empty;
    public string CardholderSurname { get; set; } = string.Empty;
    public string CardholderType { get; set; } = string.Empty;
    public string CardholderPhotoName { get; set; } = string.Empty;
    public string? CardholderPhotoRRSKey { get; set; }
    public string? CardholderPhotoURL { get; set; }
    public string? CardholderPhotoKey { get; set; }
}