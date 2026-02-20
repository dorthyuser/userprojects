using System;
using System.Collections.Generic;
using System.Linq;
using CreateTravelcard.Models;

namespace CreateTravelcard.Validators
{
    public class TravelcardValidator
    {
        public IEnumerable<ErrorItem> Validate(TravelcardRequest req)
        {
            var errors = new List<ErrorItem>();
            if (req == null)
            {
                errors.Add(new ErrorItem { Code = "InvalidPayload", Field = "body", Message = "Payload is required." });
                return errors;
            }

            // travelcardType required - enum default may be 0; assume valid if within enum; further checks done in business rules
            // travelcardValidFrom required
            if (req.TravelcardValidFrom == default)
            {
                errors.Add(new ErrorItem { Code = "MissingField", Field = "travelcardValidFrom", Message = "travelcardValidFrom is required." });
            }
            if (req.TravelcardValidTo == default)
            {
                errors.Add(new ErrorItem { Code = "MissingField", Field = "travelcardValidTo", Message = "travelcardValidTo is required." });
            }
            if (string.IsNullOrEmpty(req.TravelcardNumber))
            {
                errors.Add(new ErrorItem { Code = "MissingField", Field = "travelcardNumber", Message = "travelcardNumber is required." });
            }
            else if (req.TravelcardNumber.Length < 11 || req.TravelcardNumber.Length > 22)
            {
                errors.Add(new ErrorItem { Code = "InvalidField", Field = "travelcardNumber", Message = "travelcardNumber must be between 11 and 22 characters." });
            }

            if (req.TravelcardRequestedDate == default)
            {
                errors.Add(new ErrorItem { Code = "MissingField", Field = "travelcardRequestedDate", Message = "travelcardRequestedDate is required." });
            }

            if (string.IsNullOrEmpty(req.TravelcardTransactionReference))
            {
                errors.Add(new ErrorItem { Code = "MissingField", Field = "travelcardTransactionReference", Message = "travelcardTransactionReference is required." });
            }
            else if (req.TravelcardTransactionReference.Length != 15)
            {
                errors.Add(new ErrorItem { Code = "InvalidField", Field = "travelcardTransactionReference", Message = "travelcardTransactionReference must be exactly 15 characters." });
            }

            if (!string.IsNullOrEmpty(req.TravelcardName) && req.TravelcardName.Length > 255)
            {
                errors.Add(new ErrorItem { Code = "InvalidField", Field = "travelcardName", Message = "travelcardName must be at most 255 characters." });
            }

            // Cardholders presence
            if (req.Cardholders == null || !req.Cardholders.Any())
            {
                errors.Add(new ErrorItem { Code = "MissingField", Field = "cardholders", Message = "At least one cardholder (Primary) is required." });
            }
            else if (req.Cardholders.Count < 1 || req.Cardholders.Count > 2)
            {
                errors.Add(new ErrorItem { Code = "InvalidField", Field = "cardholders", Message = "Number of cardholders must be 1 or 2." });
            }

            if (req.Cardholders != null)
            {
                for (int i = 0; i < req.Cardholders.Count; i++)
                {
                    var ch = req.Cardholders[i];
                    var prefix = $"cardholders[{i}]";
                    if (string.IsNullOrEmpty(ch.CardholderTitle) || ch.CardholderTitle.Length < 1 || ch.CardholderTitle.Length > 15)
                    {
                        errors.Add(new ErrorItem { Code = "InvalidField", Field = prefix + ".cardholderTitle", Message = "cardholderTitle is required and must be between 1 and 15 characters." });
                    }
                    if (string.IsNullOrEmpty(ch.CardholderForename) || ch.CardholderForename.Length < 1 || ch.CardholderForename.Length > 100)
                    {
                        errors.Add(new ErrorItem { Code = "InvalidField", Field = prefix + ".cardholderForename", Message = "cardholderForename is required and must be between 1 and 100 characters." });
                    }
                    if (string.IsNullOrEmpty(ch.CardholderSurname) || ch.CardholderSurname.Length < 1 || ch.CardholderSurname.Length > 100)
                    {
                        errors.Add(new ErrorItem { Code = "InvalidField", Field = prefix + ".cardholderSurname", Message = "cardholderSurname is required and must be between 1 and 100 characters." });
                    }
                    if (string.IsNullOrEmpty(ch.CardholderPhotoName) || ch.CardholderPhotoName.Length < 1 || ch.CardholderPhotoName.Length > 100)
                    {
                        errors.Add(new ErrorItem { Code = "InvalidField", Field = prefix + ".cardholderPhotoName", Message = "cardholderPhotoName is required and must be between 1 and 100 characters." });
                    }

                    // Exactly one image detail
                    var provided = 0;
                    if (!string.IsNullOrEmpty(ch.CardholderPhotoRRSKey)) provided++;
                    if (!string.IsNullOrEmpty(ch.CardholderPhotoURL)) provided++;
                    if (!string.IsNullOrEmpty(ch.CardholderPhotoKey)) provided++;
                    if (provided != 1)
                    {
                        errors.Add(new ErrorItem { Code = "InvalidField", Field = prefix + ".image", Message = "Exactly one image detail must be provided: cardholderPhotoRRSKey or cardholderPhotoURL or cardholderPhotoKey." });
                    }
                    if (!string.IsNullOrEmpty(ch.CardholderPhotoRRSKey) && (ch.CardholderPhotoRRSKey.Length < 39 || ch.CardholderPhotoRRSKey.Length > 42))
                    {
                        errors.Add(new ErrorItem { Code = "InvalidField", Field = prefix + ".cardholderPhotoRRSKey", Message = "cardholderPhotoRRSKey must be between 39 and 42 characters." });
                    }
                    if (!string.IsNullOrEmpty(ch.CardholderPhotoKey) && (ch.CardholderPhotoKey.Length < 39 || ch.CardholderPhotoKey.Length > 42))
                    {
                        errors.Add(new ErrorItem { Code = "InvalidField", Field = prefix + ".cardholderPhotoKey", Message = "cardholderPhotoKey must be between 39 and 42 characters." });
                    }
                    if (!string.IsNullOrEmpty(ch.CardholderPhotoURL) && (ch.CardholderPhotoURL.Length < 20 || ch.CardholderPhotoURL.Length > 2048))
                    {
                        errors.Add(new ErrorItem { Code = "InvalidField", Field = prefix + ".cardholderPhotoURL", Message = "cardholderPhotoURL must be between 20 and 2048 characters." });
                    }
                }

                // Exactly one Primary
                var primaryCount = req.Cardholders.Count(c => c.CardholderType == CardholderType.Primary);
                if (primaryCount != 1)
                {
                    errors.Add(new ErrorItem { Code = "InvalidField", Field = "cardholders", Message = "Exactly one Primary cardholder must be provided." });
                }
            }

            return errors;
        }

        public IEnumerable<ErrorItem> ValidateBusinessRules(TravelcardRequest req)
        {
            var errors = new List<ErrorItem>();
            var now = DateTime.UtcNow;
            var creationDate = now.Date;
            var validFromLatestAllowed = creationDate.AddMonths(1);

            if (req.TravelcardValidFrom > validFromLatestAllowed)
            {
                errors.Add(new ErrorItem { Code = "BusinessRule", Field = "travelcardValidFrom", Message = "travelcardValidFrom must not be later than one calendar month from creation date." });
            }

            if (req.TravelcardValidFrom > req.TravelcardValidTo)
            {
                errors.Add(new ErrorItem { Code = "BusinessRule", Field = "travelcardValidFrom, travelcardValidTo", Message = "travelcardValidFrom must not be later than travelcardValidTo." });
            }

            if (req.TravelcardValidTo <= now)
            {
                errors.Add(new ErrorItem { Code = "BusinessRule", Field = "travelcardValidTo", Message = "travelcardValidTo must be in the future." });
            }

            if (req.TravelcardRequestedDate >= now)
            {
                errors.Add(new ErrorItem { Code = "BusinessRule", Field = "travelcardRequestedDate", Message = "travelcardRequestedDate must be in the past." });
            }

            if (req.TravelcardType == TravelcardType.SixteenToSeventeen)
            {
                if (!req.TravelcardUsableTo.HasValue)
                {
                    errors.Add(new ErrorItem { Code = "BusinessRule", Field = "travelcardUsableTo", Message = "travelcardUsableTo is required for SixteenToSeventeen travelcards." });
                }
                else if (req.TravelcardUsableTo.Value <= now)
                {
                    errors.Add(new ErrorItem { Code = "BusinessRule", Field = "travelcardUsableTo", Message = "travelcardUsableTo must be in the future." });
                }
            }
            else
            {
                if (req.TravelcardUsableTo.HasValue && req.TravelcardUsableTo.Value <= now)
                {
                    errors.Add(new ErrorItem { Code = "BusinessRule", Field = "travelcardUsableTo", Message = "If provided, travelcardUsableTo must be in the future." });
                }
            }

            // Secondary allowed only for certain types
            var hasSecondary = req.Cardholders != null && req.Cardholders.Any(c => c.CardholderType == CardholderType.Secondary);
            if (hasSecondary)
            {
                var allowed = new[] { TravelcardType.TwoTogether, TravelcardType.Family };
                if (!allowed.Contains(req.TravelcardType))
                {
                    errors.Add(new ErrorItem { Code = "BusinessRule", Field = "cardholders", Message = "Secondary cardholder is not allowed for the selected travelcardType." });
                }
            }

            return errors;
        }
    }
}
