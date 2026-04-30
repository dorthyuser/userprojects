using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using azurefunction318.Models;
using azurefunction318.Repositories;

namespace azurefunction318.Services
{
    public class TravelcardService : ITravelcardService
    {
        private readonly ITravelcardRepository _repo;
        private readonly ILogger<TravelcardService> _logger;

        private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "Young","Barcklays","DevonandCornwall","TwoTogether","Family","Senior","DisabledPersons","Network","TwentySixToThirty","SixteenToSeventeen","Veterans"
        };

        // Types that allow a secondary cardholder (business decision, configurable if needed)
        private static readonly HashSet<string> AllowSecondary = new(StringComparer.OrdinalIgnoreCase)
        {
            "Family","TwoTogether"
        };

        public TravelcardService(ITravelcardRepository repo, ILogger<TravelcardService> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<TravelcardResponse> CreateAsync(TravelcardRequest request, string clientId, string correlationId)
        {
            Validate(request);

            // Map and persist
            var travelcardGuid = Guid.NewGuid().ToString();
            var token = GenerateToken(6);

            try
            {
                await _repo.InsertAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error persisting travelcard to database");
                throw;
            }

            return new TravelcardResponse { TravelcardId = travelcardGuid, Token = token };
        }

        private void Validate(TravelcardRequest req)
        {
            if (req == null) throw new ValidationException("Request body is required");

            if (string.IsNullOrWhiteSpace(req.TravelcardType) || !AllowedTypes.Contains(req.TravelcardType))
                throw new ValidationException("Invalid or missing travelcardType");

            if (req.TravelcardRequestedDate == default)
                throw new ValidationException("travelcardRequestedDate is required");

            if (req.TravelcardRequestedDate > DateTime.UtcNow.AddMinutes(1))
                throw new ValidationException("travelcardRequestedDate must be in the past");

            if (req.TravelcardValidFrom == default || req.TravelcardValidTo == default)
                throw new ValidationException("travelcardValidFrom and travelcardValidTo are required");

            if (req.TravelcardValidFrom >= req.TravelcardValidTo)
                throw new ValidationException("travelcardValidFrom must be earlier than travelcardValidTo");

            if (req.TravelcardValidTo <= DateTime.UtcNow)
                throw new ValidationException("travelcardValidTo must be in the future");

            if (string.IsNullOrWhiteSpace(req.TravelcardNumber) || req.TravelcardNumber.Length < 11 || req.TravelcardNumber.Length > 22)
                throw new ValidationException("travelcardNumber is required and must be between 11 and 22 characters");

            if (string.IsNullOrWhiteSpace(req.TravelcardTransactionReference) || req.TravelcardTransactionReference.Length != 15)
                throw new ValidationException("travelcardTransactionReference is required and must be 15 characters");

            if (req.TravelcardType.Equals("SixteenToSeventeen", StringComparison.OrdinalIgnoreCase))
            {
                if (!req.TravelcardUsableTo.HasValue)
                    throw new ValidationException("travelcardUsableTo is required for SixteenToSeventeen travelcards");

                if (req.TravelcardUsableTo.Value <= DateTime.UtcNow)
                    throw new ValidationException("travelcardUsableTo must be in the future");
            }

            if (req.Cardholders == null || req.Cardholders.Count < 1 || req.Cardholders.Count > 2)
                throw new ValidationException("cardholders must contain 1 or 2 items");

            var primaryCount = req.Cardholders.Count(c => string.Equals(c.CardholderType, "Primary", StringComparison.OrdinalIgnoreCase));
            if (primaryCount != 1) throw new ValidationException("Exactly one Primary cardholder is required");

            if (req.Cardholders.Count == 2)
            {
                var hasSecondary = req.Cardholders.Any(c => string.Equals(c.CardholderType, "Secondary", StringComparison.OrdinalIgnoreCase));
                if (!hasSecondary) throw new ValidationException("If two cardholders are present one must be Secondary");

                if (!AllowSecondary.Contains(req.TravelcardType))
                    throw new ValidationException($"Travelcard type '{req.TravelcardType}' does not allow a Secondary cardholder");
            }

            foreach (var card in req.Cardholders)
            {
                if (string.IsNullOrWhiteSpace(card.CardholderTitle) || card.CardholderTitle.Length > 15)
                    throw new ValidationException("cardholderTitle is required and must be <= 15 characters");

                if (string.IsNullOrWhiteSpace(card.CardholderForename) || card.CardholderForename.Length > 100)
                    throw new ValidationException("cardholderForename is required and must be <= 100 characters");

                if (string.IsNullOrWhiteSpace(card.CardholderSurname) || card.CardholderSurname.Length > 100)
                    throw new ValidationException("cardholderSurname is required and must be <= 100 characters");

                if (string.IsNullOrWhiteSpace(card.CardholderPhotoName) || card.CardholderPhotoName.Length > 100)
                    throw new ValidationException("cardholderPhotoName is required and must be <= 100 characters");

                // Exactly one of the photo fields must be provided
                var photoSet = 0;
                if (!string.IsNullOrWhiteSpace(card.CardholderPhotoRRSKey)) photoSet++;
                if (!string.IsNullOrWhiteSpace(card.CardholderPhotoURL)) photoSet++;
                if (!string.IsNullOrWhiteSpace(card.CardholderPhotoKey)) photoSet++;
                if (photoSet != 1) throw new ValidationException("Each cardholder must have exactly one of cardholder_photo_rrs_key, cardholder_photo_url or cardholder_photo_key");

                if (!string.IsNullOrWhiteSpace(card.CardholderPhotoRRSKey) && (card.CardholderPhotoRRSKey.Length < 39 || card.CardholderPhotoRRSKey.Length > 42))
                    throw new ValidationException("cardholderPhotoRRSKey must be between 39 and 42 characters if provided");

                if (!string.IsNullOrWhiteSpace(card.CardholderPhotoKey) && (card.CardholderPhotoKey.Length < 39 || card.CardholderPhotoKey.Length > 42))
                    throw new ValidationException("cardholderPhotoKey must be between 39 and 42 characters if provided");

                if (!string.IsNullOrWhiteSpace(card.CardholderPhotoURL) && (card.CardholderPhotoURL.Length < 20 || card.CardholderPhotoURL.Length > 2048))
                    throw new ValidationException("cardholderPhotoURL must be between 20 and 2048 characters if provided");
            }
        }

        private static string GenerateToken(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var rnd = new Random();
            var token = new char[length];
            for (int i = 0; i < length; i++) token[i] = chars[rnd.Next(chars.Length)];
            return new string(token);
        }
    }
}
