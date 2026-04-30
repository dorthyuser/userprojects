using System;
using System.Collections.Generic;

namespace azurefunction318.Models
{
    public class TravelcardRequest
    {
        public string TravelcardType { get; set; }
        public DateTime TravelcardValidFrom { get; set; }
        public DateTime TravelcardValidTo { get; set; }
        public string TravelcardName { get; set; }
        public string TravelcardNumber { get; set; }
        public DateTime TravelcardRequestedDate { get; set; }
        public string TravelcardTransactionReference { get; set; }
        public DateTime? TravelcardUsableTo { get; set; }
        public List<CardholderRequest> Cardholders { get; set; }
    }
}
