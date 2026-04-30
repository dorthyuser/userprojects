namespace azuresharpapi153.Models.Entities;

public sealed class CardholderEntity
{
    public int Id { get; set; }
    public int TravelcardId { get; set; }
    public string CardholderTitle { get; set; } = string.Empty;
    public string CardholderForename { get; set; } = string.Empty;
    public string CardholderSurname { get; set; } = string.Empty;
    public CardholderTypeEnum CardholderType { get; set; }
    public string CardholderPhotoName { get; set; } = string.Empty;
    public string? CardholderPhotoRRSKey { get; set; }
    public string? CardholderPhotoURL { get; set; }
    public string? CardholderPhotoKey { get; set; }
    public TravelcardEntity? Travelcard { get; set; }
}