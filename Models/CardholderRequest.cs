using System;

namespace azuretravelcardfunction309.Models;

public sealed class CardholderRequest
{
    public string cardholderTitle { get; set; }
    public string cardholderForename { get; set; }
    public string cardholderSurname { get; set; }
    public cardholderType_enum cardholderType { get; set; }
    public string cardholderPhotoName { get; set; }
    public string? cardholderPhotoRRSKey { get; set; }
    public string? cardholderPhotoURL { get; set; }
    public string? cardholderPhotoKey { get; set; }

    public CardholderRequest()
    {
        cardholderTitle = string.Empty;
        cardholderForename = string.Empty;
        cardholderSurname = string.Empty;
        cardholderPhotoName = string.Empty;
    }
}
