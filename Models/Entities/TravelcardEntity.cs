namespace azuresharpapi153.Models.Entities;

public sealed class TravelcardEntity
{
    public int Id { get; set; }
    public TravelcardTypeEnum TravelcardType { get; set; }
    public DateTimeOffset TravelcardValidFrom { get; set; }
    public DateTimeOffset TravelcardValidTo { get; set; }
    public string? TravelcardName { get; set; }
    public string? TravelcardNumber { get; set; }
    public DateTimeOffset TravelcardRequestedDate { get; set; }
    public string TravelcardTransactionReference { get; set; } = string.Empty;
    public DateTimeOffset? TravelcardUsableTo { get; set; }
    public ICollection<CardholderEntity> Cardholders { get; set; } = new List<CardholderEntity>();
}