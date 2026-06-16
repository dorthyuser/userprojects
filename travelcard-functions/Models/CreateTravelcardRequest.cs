using System;
using System.Collections.Generic;

namespace travelcard_functions.Models;

public class CreateTravelcardRequest
{
    public TravelcardType TravelcardType { get; set; }
    public DateTime TravelcardValidFrom { get; set; }
    public DateTime TravelcardValidTo { get; set; }
    public string? TravelcardName { get; set; }
    public string TravelcardNumber { get; set; } = string.Empty;
    public DateTime TravelcardRequestedDate { get; set; }
    public string TravelcardTransactionReference { get; set; } = string.Empty;
    public DateTime? TravelcardUsableTo { get; set; }
    public List<CardholderRequest> Cardholders { get; set; } = new();
}
