namespace azuresharpapi153.Models.Requests;

public sealed class CreateTravelcardRequest
{
    public string TravelcardType { get; set; } = string.Empty;
    public DateTimeOffset TravelcardValidFrom { get; set; }
    public DateTimeOffset TravelcardValidTo { get; set; }
    public string? TravelcardName { get; set; }
    public string TravelcardNumber { get; set; } = string.Empty;
    public DateTimeOffset TravelcardRequestedDate { get; set; }
    public string TravelcardTransactionReference { get; set; } = string.Empty;
    public DateTimeOffset? TravelcardUsableTo { get; set; }
    public List<CardholderRequest> Cardholders { get; set; } = new();
}