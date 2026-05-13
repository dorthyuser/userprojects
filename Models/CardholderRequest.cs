namespace travelcardlambdachsarp549.Models
{
    public sealed class CardholderRequest
    {
        public string CardholderTitle { get; set; } = string.Empty;
        public string CardholderForename { get; set; } = string.Empty;
        public string CardholderSurname { get; set; } = string.Empty;
        public cardholder_type_enum CardholderType { get; set; }
        public string CardholderPhotoName { get; set; } = string.Empty;
        public string? CardholderPhotoRRSKey { get; set; }
        public string? CardholderPhotoURL { get; set; }
        public string? CardholderPhotoKey { get; set; }
    }
}