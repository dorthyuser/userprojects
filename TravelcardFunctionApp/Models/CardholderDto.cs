namespace TravelcardFunctionApp.Models
{
    public class CardholderDto
    {
        public string? CardholderTitle { get; set; }
        public string? CardholderForename { get; set; }
        public string? CardholderSurname { get; set; }
        public CardholderType CardholderType { get; set; }
        public string? CardholderPhotoName { get; set; }
        public string? CardholderPhotoRrsKey { get; set; }
        public string? CardholderPhotoUrl { get; set; }
        public string? CardholderPhotoKey { get; set; }
    }
}
