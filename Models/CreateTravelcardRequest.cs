namespace azurecsharppost1112.Models;

public sealed class CreateTravelcardRequest
{
    public travelcardType_enum travelcardType { get; set; }
    public DateTime travelcardValidFrom { get; set; }
    public DateTime travelcardValidTo { get; set; }
    public string? travelcardName { get; set; }
    public string travelcardNumber { get; set; } = string.Empty;
    public DateTime travelcardRequestedDate { get; set; }
    public string travelcardTransactionReference { get; set; } = string.Empty;
    public DateTime? travelcardUsableTo { get; set; }
    public List<CardholderRequest> cardholders { get; set; } = new();
}
