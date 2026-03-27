using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TravelcardFunctionApp.Models;

namespace TravelcardFunctionApp.Helpers
{
    public class ValidationHelper
    {
        private static readonly HashSet<TravelcardType> SecondaryAllowed = new HashSet<TravelcardType> { TravelcardType.Family, TravelcardType.TwoTogether };

        public List<string> Validate(TravelcardRequest request)
        {
            var errors = new List<string>();
            var now = DateTime.UtcNow;

            // requested_date in the past
            if (request.TravelcardRequestedDate > now)
            {
                errors.Add("travelcardRequestedDate must be in the past");
            }

            // valid_from earlier than valid_to
            if (request.TravelcardValidFrom >= request.TravelcardValidTo)
            {
                errors.Add("travelcardValidFrom must be earlier than travelcardValidTo");
            }

            // valid_to in the future
            if (request.TravelcardValidTo <= now)
            {
                errors.Add("travelcardValidTo must be in the future");
            }

            // validFrom no later than one calendar month from creation
            if (request.TravelcardValidFrom > now.AddMonths(1))
            {
                errors.Add("travelcardValidFrom must be no later than one calendar month from creation");
            }

            // transaction reference length == 15
            if (string.IsNullOrWhiteSpace(request.TravelcardTransactionReference) || request.TravelcardTransactionReference.Length != 15)
            {
                errors.Add("travelcardTransactionReference must be exactly 15 characters");
            }

            // travelcardNumber length
            if (string.IsNullOrWhiteSpace(request.TravelcardNumber) || request.TravelcardNumber.Length < 11 || request.TravelcardNumber.Length > 22)
            {
                errors.Add("travelcardNumber must be between 11 and 22 characters");
            }

            // UsableTo required for SixteenToSeventeen
            if (request.TravelcardType == TravelcardType.SixteenToSeventeen)
            {
                if (!request.TravelcardUsableTo.HasValue)
                {
                    errors.Add("travelcardUsableTo is required for SixteenToSeventeen travelcard type");
                }
                else if (request.TravelcardUsableTo <= now)
                {
                    errors.Add("travelcardUsableTo must be in the future");
                }
            }
            else
            {
                if (request.TravelcardUsableTo.HasValue && request.TravelcardUsableTo <= now)
                {
                    errors.Add("travelcardUsableTo, if provided, must be in the future");
                }
            }

            // Cardholders count 1 or 2
            if (request.Cardholders == null || request.Cardholders.Count < 1 || request.Cardholders.Count > 2)
            {
                errors.Add("cardholders must contain exactly one or two items");
            }
            else
            {
                // exactly one Primary
                var primaryCount = request.Cardholders.Count(c => c.CardholderType == CardholderType.Primary);
                if (primaryCount != 1)
                {
                    errors.Add("There must be exactly one Primary cardholder");
                }

                // secondary allowed only for certain travelcard types
                if (request.Cardholders.Any(c => c.CardholderType == CardholderType.Secondary) && !SecondaryAllowed.Contains(request.TravelcardType))
                {
                    errors.Add("Secondary cardholder is not allowed for this travelcard type");
                }

                // validate each cardholder
                foreach (var ch in request.Cardholders)
                {
                    if (string.IsNullOrWhiteSpace(ch.CardholderTitle) || ch.CardholderTitle.Length > 15)
                    {
                        errors.Add("cardholderTitle is required and must be <= 15 characters");
                    }

                    if (string.IsNullOrWhiteSpace(ch.CardholderForename) || ch.CardholderForename.Length > 100)
                    {
                        errors.Add("cardholderForename is required and must be <= 100 characters");
                    }

                    if (string.IsNullOrWhiteSpace(ch.CardholderSurname) || ch.CardholderSurname.Length > 100)
                    {
                        errors.Add("cardholderSurname is required and must be <= 100 characters");
                    }

                    if (string.IsNullOrWhiteSpace(ch.CardholderPhotoName) || ch.CardholderPhotoName.Length > 100)
                    {
                        errors.Add("cardholderPhotoName is required and must be <= 100 characters");
                    }

                    // OneOf: at least one of PhotoRRSKey, PhotoURL, PhotoKey
                    if (string.IsNullOrWhiteSpace(ch.CardholderPhotoRRSKey) && string.IsNullOrWhiteSpace(ch.CardholderPhotoURL) && string.IsNullOrWhiteSpace(ch.CardholderPhotoKey))
                    {
                        errors.Add("Each cardholder must provide one of cardholderPhotoRRSKey, cardholderPhotoURL, cardholderPhotoKey");
                    }

                    // If RRSKey or PhotoKey provided, check lengths (>=39 <=42)
                    if (!string.IsNullOrWhiteSpace(ch.CardholderPhotoRRSKey) && (ch.CardholderPhotoRRSKey.Length < 39 || ch.CardholderPhotoRRSKey.Length > 42))
                    {
                        errors.Add("cardholderPhotoRRSKey must be between 39 and 42 characters if provided");
                    }

                    if (!string.IsNullOrWhiteSpace(ch.CardholderPhotoKey) && (ch.CardholderPhotoKey.Length < 39 || ch.CardholderPhotoKey.Length > 42))
                    {
                        errors.Add("cardholderPhotoKey must be between 39 and 42 characters if provided");
                    }

                    if (!string.IsNullOrWhiteSpace(ch.CardholderPhotoURL))
                    {
                        if (ch.CardholderPhotoURL.Length < 20 || ch.CardholderPhotoURL.Length > 2048)
                        {
                            errors.Add("cardholderPhotoURL must be between 20 and 2048 characters if provided");
                        }
                        else
                        {
                            if (!Uri.IsWellFormedUriString(ch.CardholderPhotoURL, UriKind.Absolute))
                            {
                                errors.Add("cardholderPhotoURL must be a valid absolute URI");
                            }
                        }
                    }

                    // Name regex check to approximate DB constraint
                    var nameRegex = new Regex(@"^(?!.*[×÷ˇ˘μ])[A-Za-zÀ-ž .'\-]+$");
                    if (!nameRegex.IsMatch(ch.CardholderForename))
                    {
                        errors.Add("cardholderForename contains invalid characters");
                    }

                    if (!nameRegex.IsMatch(ch.CardholderSurname))
                    {
                        errors.Add("cardholderSurname contains invalid characters");
                    }
                }
            }

            return errors;
        }
    }
}
