using System;
using System.Collections.Generic;
using System.Linq;
using CreateTravelcard.Models;
using CreateTravelcard.Validators;
using Xunit;

namespace CreateTravelcard.Tests
{
    public class TravelcardValidatorTests
    {
        private readonly TravelcardValidator _validator = new TravelcardValidator();

        private Cardholder MakeCardholder(CardholderType type = CardholderType.Primary, string photoUrl = null, string photoKey = null, string photoRrsKey = null)
        {
            return new Cardholder
            {
                CardholderTitle = "Mr",
                CardholderForename = "John",
                CardholderSurname = "Doe",
                CardholderType = type,
                CardholderPhotoName = "photo.jpg",
                CardholderPhotoURL = photoUrl,
                CardholderPhotoKey = photoKey,
                CardholderPhotoRRSKey = photoRrsKey
            };
        }

        private TravelcardRequest MakeValidRequest()
        {
            var now = DateTime.UtcNow;
            return new TravelcardRequest
            {
                TravelcardType = TravelcardType.Family,
                TravelcardValidFrom = now.Date.AddDays(1),
                TravelcardValidTo = now.Date.AddMonths(2),
                TravelcardName = "Valid Name",
                TravelcardNumber = "12345678901", // 11 chars
                TravelcardRequestedDate = now.AddDays(-1),
                TravelcardTransactionReference = new string('A', 15),
                TravelcardUsableTo = now.Date.AddYears(1),
                Cardholders = new List<Cardholder> { MakeCardholder(photoUrl: "https://example.com/this-is-a-valid-url-with-more-than-20-chars") }
            };
        }

        [Fact]
        public void Validate_NullPayload_ReturnsInvalidPayloadError()
        {
            var errors = _validator.Validate(null).ToList();
            Assert.Single(errors);
            Assert.Equal("InvalidPayload", errors[0].Code);
            Assert.Equal("body", errors[0].Field);
        }

        [Fact]
        public void Validate_MissingRequiredFields_ReturnsExpectedFieldErrors()
        {
            var req = new TravelcardRequest();
            var errors = _validator.Validate(req).ToList();

            // Expect missing travelcardValidFrom
            Assert.Contains(errors, e => e.Field == "travelcardValidFrom" && e.Code == "MissingField");
            Assert.Contains(errors, e => e.Field == "travelcardValidTo" && e.Code == "MissingField");
            Assert.Contains(errors, e => e.Field == "travelcardNumber" && e.Code == "MissingField");
            Assert.Contains(errors, e => e.Field == "travelcardRequestedDate" && e.Code == "MissingField");
            Assert.Contains(errors, e => e.Field == "travelcardTransactionReference" && e.Code == "MissingField");
            Assert.Contains(errors, e => e.Field == "cardholders" && e.Code == "MissingField");
        }

        [Fact]
        public void Validate_ValidSinglePrimary_NoErrors()
        {
            var req = MakeValidRequest();
            var errors = _validator.Validate(req).ToList();
            Assert.Empty(errors);
        }

        [Fact]
        public void ValidateBusinessRules_RequestedDateInFuture_ReturnsError()
        {
            var req = MakeValidRequest();
            req.TravelcardRequestedDate = DateTime.UtcNow.AddMinutes(10); // in future
            var errors = _validator.ValidateBusinessRules(req).ToList();
            Assert.Contains(errors, e => e.Field == "travelcardRequestedDate" && e.Code == "BusinessRule");
        }

        [Fact]
        public void ValidateBusinessRules_ValidFromTooLate_ReturnsError()
        {
            var req = MakeValidRequest();
            // creationDate is now.Date; set validFrom beyond one calendar month
            req.TravelcardValidFrom = DateTime.UtcNow.Date.AddMonths(2);
            var errors = _validator.ValidateBusinessRules(req).ToList();
            Assert.Contains(errors, e => e.Field == "travelcardValidFrom" && e.Code == "BusinessRule");
        }

        [Fact]
        public void ValidateBusinessRules_SixteenToSeventeen_RequiresUsableTo()
        {
            var req = MakeValidRequest();
            req.TravelcardType = TravelcardType.SixteenToSeventeen;
            req.TravelcardUsableTo = null;
            var errors = _validator.ValidateBusinessRules(req).ToList();
            Assert.Contains(errors, e => e.Field == "travelcardUsableTo" && e.Code == "BusinessRule");

            // now provide usableTo but in the past
            req.TravelcardUsableTo = DateTime.UtcNow.AddDays(-1);
            errors = _validator.ValidateBusinessRules(req).ToList();
            Assert.Contains(errors, e => e.Field == "travelcardUsableTo" && e.Code == "BusinessRule");
        }

        [Fact]
        public void ValidateBusinessRules_SecondaryNotAllowed_ReturnsError()
        {
            var req = MakeValidRequest();
            // add a secondary cardholder while TravelcardType.Family allows secondary; switch to a type that does not allow
            req.TravelcardType = TravelcardType.Young;
            req.Cardholders = new List<Cardholder> { MakeCardholder(CardholderType.Primary), MakeCardholder(CardholderType.Secondary, photoUrl: "https://example.com/this-is-a-valid-url-with-more-than-20-chars") };
            var errors = _validator.ValidateBusinessRules(req).ToList();
            Assert.Contains(errors, e => e.Field == "cardholders" && e.Code == "BusinessRule");
        }

        [Fact]
        public void Validate_ExactlyOneImageProvided_ValidationBehavior()
        {
            var req = MakeValidRequest();
            // Make cardholder with no image detail -> invalid
            req.Cardholders = new List<Cardholder> { MakeCardholder(photoUrl: null, photoKey: null, photoRrsKey: null) };
            var errors = _validator.Validate(req).ToList();
            Assert.Contains(errors, e => e.Field.StartsWith("cardholders[0].image") && e.Code == "InvalidField");

            // Provide two image details -> invalid
            req.Cardholders = new List<Cardholder> { MakeCardholder(photoUrl: "https://example.com/valid-url", photoRrsKey: "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa") };
            // above has two image fields -> should still produce image error
            errors = _validator.Validate(req).ToList();
            Assert.Contains(errors, e => e.Field.StartsWith("cardholders[0].image") && e.Code == "InvalidField");
        }
    }
}
