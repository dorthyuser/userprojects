using azuresharpapi153.Data;
using azuresharpapi153.Models.Entities;
using azuresharpapi153.Models.Requests;
using azuresharpapi153.Models.Responses;
using Microsoft.EntityFrameworkCore;

namespace azuresharpapi153.Services;

public sealed class TravelcardService : ITravelcardService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<TravelcardService> _logger;

    public TravelcardService(AppDbContext dbContext, ILogger<TravelcardService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<CreateTravelcardResponse> CreateAsync(CreateTravelcardRequest request, string? correlationId, CancellationToken cancellationToken)
    {
        ValidateRequest(request);

        var travelcard = new TravelcardEntity
        {
            TravelcardType = Enum.Parse<TravelcardTypeEnum>(request.TravelcardType, true),
            TravelcardValidFrom = request.TravelcardValidFrom,
            TravelcardValidTo = request.TravelcardValidTo,
            TravelcardName = string.IsNullOrWhiteSpace(request.TravelcardName) ? request.TravelcardType : request.TravelcardName,
            TravelcardNumber = request.TravelcardNumber,
            TravelcardRequestedDate = request.TravelcardRequestedDate,
            TravelcardTransactionReference = request.TravelcardTransactionReference,
            TravelcardUsableTo = request.TravelcardUsableTo
        };

        travelcard.Cardholders = request.Cardholders.Select(x => new CardholderEntity
        {
            CardholderTitle = x.CardholderTitle,
            CardholderForename = x.CardholderForename,
            CardholderSurname = x.CardholderSurname,
            CardholderType = Enum.Parse<CardholderTypeEnum>(x.CardholderType, true),
            CardholderPhotoName = x.CardholderPhotoName,
            CardholderPhotoRRSKey = x.CardholderPhotoRRSKey,
            CardholderPhotoURL = x.CardholderPhotoURL,
            CardholderPhotoKey = x.CardholderPhotoKey
        }).ToList();

        _dbContext.Travelcards.Add(travelcard);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var token = GenerateToken();
        _logger.LogInformation("Travelcard created successfully. CorrelationId={CorrelationId}", correlationId);

        return new CreateTravelcardResponse
        {
            TravelcardId = Guid.NewGuid().ToString(),
            Token = token
        };
    }

    private static void ValidateRequest(CreateTravelcardRequest request)
    {
        if (request.TravelcardRequestedDate >= DateTimeOffset.UtcNow)
        {
            throw new ArgumentException("travelcardRequestedDate must be in the past.");
        }

        if (request.TravelcardValidFrom > request.TravelcardValidTo)
        {
            throw new ArgumentException("travelcardValidFrom must be later than travelcardValidTo.");
        }

        if (request.TravelcardValidTo <= DateTimeOffset.UtcNow)
        {
            throw new ArgumentException("travelcardValidTo must be in the future.");
        }

        var type = Enum.Parse<TravelcardTypeEnum>(request.TravelcardType, true);
        if (type == TravelcardTypeEnum.SixteenToSeventeen)
        {
            if (!request.TravelcardUsableTo.HasValue)
            {
                throw new ArgumentException("travelcardUsableTo is required for SixteenToSeventeen travelcards.");
            }

            if (request.TravelcardUsableTo.Value <= DateTimeOffset.UtcNow)
            {
                throw new ArgumentException("travelcardUsableTo must be in the future.");
            }
        }

        if (request.Cardholders.Count < 1 || request.Cardholders.Count > 2)
        {
            throw new ArgumentException("cardholders must contain exactly one or two items.");
        }

        var primaryCount = request.Cardholders.Count(x => string.Equals(x.CardholderType, "Primary", StringComparison.OrdinalIgnoreCase));
        var secondaryCount = request.Cardholders.Count(x => string.Equals(x.CardholderType, "Secondary", StringComparison.OrdinalIgnoreCase));

        if (primaryCount != 1)
        {
            throw new ArgumentException("Exactly one Primary cardholder is required.");
        }

        if (secondaryCount > 1)
        {
            throw new ArgumentException("Only one Secondary cardholder is allowed.");
        }

        if (secondaryCount == 1 && type is TravelcardTypeEnum.SixteenToSeventeen or TravelcardTypeEnum.Veterans)
        {
            throw new ArgumentException("Secondary cardholder is not allowed for this travelcard type.");
        }

        foreach (var cardholder in request.Cardholders)
        {
            if (!HasOnePhotoIdentifier(cardholder))
            {
                throw new ArgumentException("Each cardholder must provide exactly one photo identifier.");
            }
        }
    }

    private static bool HasOnePhotoIdentifier(CardholderRequest cardholder)
    {
        var count = 0;
        if (!string.IsNullOrWhiteSpace(cardholder.CardholderPhotoRRSKey)) count++;
        if (!string.IsNullOrWhiteSpace(cardholder.CardholderPhotoURL)) count++;
        if (!string.IsNullOrWhiteSpace(cardholder.CardholderPhotoKey)) count++;
        return count == 1;
    }

    private static string GenerateToken() => Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("=", string.Empty).Replace("+", string.Empty).Replace("/", string.Empty)[..6].ToUpperInvariant();
}