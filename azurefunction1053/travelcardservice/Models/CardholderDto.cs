namespace travelcardservice.Models
{
    public class CardholderDto
    {
        public string CardholderTitle { get; set; } = string.Empty;
        public string CardholderForename { get; set; } = string.Empty;
        public string CardholderSurname { get; set; } = string.Empty;
        public CardholderType CardholderType { get; set; } = CardholderType.Primary;
        public string CardholderPhotoName { get; set; } = string.Empty;
        public string CardholderPhotoRRSKey { get; set; } = string.Empty;
        public string CardholderPhotoURL { get; set; } = string.Empty;
        public string CardholderPhotoKey { get; set; } = string.Empty;
    }
}
