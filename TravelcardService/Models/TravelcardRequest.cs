using System;
using System.Text.Json.Serialization;

namespace TravelcardService.Models
{
    public class TravelcardRequest
    {
        // All fields are nullable to avoid CS8618 warnings and to allow flexible payloads
        public string? PassengerName { get; set; }
        public string? Origin { get; set; }
        public string? Destination { get; set; }
        public DateTime? TravelDate { get; set; }
        public TravelcardType? CardType { get; set; }
    }
}
