using System;
using System.Collections.Generic;

namespace azuretravelcardfunction309.Models;

public sealed class CreateTravelcardRequest
{
    public travelcardType_enum travelcardType { get; set; }
    public DateTime travelcardValidFrom { get; set; }
    public DateTime travelcardValidTo { get; set; }
    public string? travelcardName { get; set; }
    public string travelcardNumber { get; set; }
    public DateTime travelcardRequestedDate { get; set; }
    public string travelcardTransactionReference { get; set; }
    public DateTime? travelcardUsableTo { get; set; }
    public List<CardholderRequest> cardholders { get; set; }

    public CreateTravelcardRequest()
    {
        travelcardNumber = string.Empty;
        travelcardTransactionReference = string.Empty;
        cardholders = new List<CardholderRequest>();
    }
}
