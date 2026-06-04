package com.ai2dev.springboottravelcardapi.service;

import com.ai2dev.springboottravelcardapi.model.CardholderEntity;
import com.ai2dev.springboottravelcardapi.model.CardholderType;
import com.ai2dev.springboottravelcardapi.model.TravelcardEntity;
import com.ai2dev.springboottravelcardapi.model.TravelcardType;
import com.ai2dev.springboottravelcardapi.model.dto.CardholderRequestDto;
import com.ai2dev.springboottravelcardapi.model.dto.TravelcardCreateRequestDto;
import com.ai2dev.springboottravelcardapi.model.dto.TravelcardCreateResponseDto;
import com.ai2dev.springboottravelcardapi.repository.CardholderRepository;
import com.ai2dev.springboottravelcardapi.repository.TravelcardRepository;
import java.time.Clock;
import java.time.OffsetDateTime;
import java.util.List;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

@Service
public class TravelcardService
{
    private static final Logger logger = LoggerFactory.getLogger(TravelcardService.class);

    private final TravelcardRepository travelcardRepository;
    private final CardholderRepository cardholderRepository;
    private final Clock clock;

    public TravelcardService(TravelcardRepository travelcardRepository, CardholderRepository cardholderRepository)
    {
        this.travelcardRepository = travelcardRepository;
        this.cardholderRepository = cardholderRepository;
        this.clock = Clock.systemUTC();
    }

    @Transactional
    public TravelcardCreateResponseDto createTravelcard(TravelcardCreateRequestDto request, String clientId, String correlationId)
    {
        logger.info("Creating travelcard record");
        validateBusinessRules(request);

        TravelcardEntity travelcard = new TravelcardEntity(
                null,
                request.travelcardType(),
                request.travelcardValidFrom(),
                request.travelcardValidTo(),
                request.travelcardName(),
                request.travelcardNumber(),
                request.travelcardRequestedDate(),
                request.travelcardTransactionReference(),
                request.travelcardUsableTo());

        TravelcardEntity savedTravelcard = travelcardRepository.save(travelcard);
        logger.info("Inserted into travelcards");

        for (CardholderRequestDto cardholderRequest : request.cardholders())
        {
            CardholderEntity cardholder = new CardholderEntity(
                    null,
                    savedTravelcard.id(),
                    cardholderRequest.cardholderTitle(),
                    cardholderRequest.cardholderForename(),
                    cardholderRequest.cardholderSurname(),
                    cardholderRequest.cardholderType(),
                    cardholderRequest.cardholderPhotoName(),
                    cardholderRequest.cardholderPhotoRRSKey(),
                    cardholderRequest.cardholderPhotoURL(),
                    cardholderRequest.cardholderPhotoKey());
            cardholderRepository.save(cardholder);
        }

        logger.info("Inserted into cardholders");
        return new TravelcardCreateResponseDto(String.valueOf(savedTravelcard.id()), generateToken(savedTravelcard.id()));
    }

    private void validateBusinessRules(TravelcardCreateRequestDto request)
    {
        OffsetDateTime now = OffsetDateTime.now(clock);
        if (request.travelcardRequestedDate().isAfter(now))
        {
            throw new IllegalArgumentException("travelcardRequestedDate must be in the past");
        }
        if (request.travelcardValidFrom().isAfter(request.travelcardValidTo()))
        {
            throw new IllegalArgumentException("travelcardValidFrom must not be later than travelcardValidTo");
        }
        if (request.travelcardValidTo().isBefore(now))
        {
            throw new IllegalArgumentException("travelcardValidTo must be in the future");
        }
        if (request.travelcardValidFrom().isAfter(now.plusMonths(1)))
        {
            throw new IllegalArgumentException("travelcardValidFrom must be no later than one calendar month from today");
        }
        if (request.travelcardType() == TravelcardType.SixteenToSeventeen && request.travelcardUsableTo() == null)
        {
            throw new IllegalArgumentException("travelcardUsableTo is required for SixteenToSeventeen travelcards");
        }
        if (request.travelcardUsableTo() != null && request.travelcardUsableTo().isBefore(now))
        {
            throw new IllegalArgumentException("travelcardUsableTo must be in the future");
        }
        if ((request.travelcardType() == TravelcardType.SixteenToSeventeen || request.travelcardType() == TravelcardType.Veterans)
                && request.cardholders().stream().anyMatch(cardholder -> cardholder.cardholderType() == CardholderType.Secondary))
        {
            throw new IllegalArgumentException("Secondary cardholder is not allowed for this travelcard type");
        }
        long primaryCount = request.cardholders().stream().filter(cardholder -> cardholder.cardholderType() == CardholderType.Primary).count();
        if (primaryCount != 1)
        {
            throw new IllegalArgumentException("Exactly one Primary cardholder is required");
        }
        if (request.cardholders().size() < 1 || request.cardholders().size() > 2)
        {
            throw new IllegalArgumentException("cardholders must contain one or two items");
        }
    }

    private String generateToken(Long id)
    {
        return "P5SSY6" + id;
    }
}
