using System;

namespace azurefunction318.Models
{
    public class CardholderRequest
    {
        public string CardholderTitle { get; set; }
        public string CardholderForename { get; set; }
        public string CardholderSurname { get; set; }
        public string CardholderType { get; set; }
        public string CardholderPhotoName { get; set; }
        public string CardholderPhotoRRSKey { get; set; }
        public string CardholderPhotoURL { get; set; }
        public string CardholderPhotoKey { get; set; }
    }
}
