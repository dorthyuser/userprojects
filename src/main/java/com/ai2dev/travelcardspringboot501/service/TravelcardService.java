package com.ai2dev.travelcardspringboot501.service;

import com.ai2dev.travelcardspringboot501.dto.CardholderRequestDto;
import com.ai2dev.travelcardspringboot501.dto.TravelcardCreateRequestDto;
import com.ai2dev.travelcardspringboot501.dto.TravelcardCreateResponseDto;
import com.ai2dev.travelcardspringboot501.model.CardholderEntity;
import com.ai2dev.travelcardspringboot501.model.CardholderTypeEnum;
import com.ai2dev.travelcardspringboot501.model.TravelcardEntity;
import com.ai2dev.travelcardspringboot501.model.TravelcardTypeEnum;
import com.ai2dev.travelcardspringboot501.repository.CardholderRepository;
import com.ai2dev.travelcardspringboot501.repository.TravelcardRepository;
import java.security.SecureRandom;
import java.util.List;
import java.util.Objects;
import java.util.UUID;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

@Service
public class TravelcardService
{
    private static final Logger logger = LoggerFactory.getLogger(TravelcardService.class);
    private static final SecureRandom secureRandom = new SecureRandom();

    private final TravelcardRepository travelcardRepository;
    private final CardholderRepository cardholderRepository;

    public TravelcardService(TravelcardRepository travelcardRepository, CardholderRepository cardholderRepository)
    {
        this.travelcardRepository = travelcardRepository;
        this.cardholderRepository = cardholderRepository;
    }

    @Transactional
    public TravelcardCreateResponseDto createTravelcard(String clientId, String correlationId, TravelcardCreateRequestDto request)
    {
        logger.info("DB operation INSERT on travelcards and cardholders");
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
        Long travelcardId = savedTravelcard.id();

        for (CardholderRequestDto cardholderRequest : request.cardholders())
        {
            CardholderEntity cardholder = new CardholderEntity(
                    null,
                    Objects.requireNonNull(travelcardId),
                    cardholderRequest.cardholderTitle(),
                    cardholderRequest.cardholderForename(),
                    cardholderRequest.cardholderSurname(),
                    cardholderRequest.cardholderType(),
                    cardholderRequest.cardholderPhotoName(),
                    cardholderRequest.cardholderPhotoRrsKey(),
                    cardholderRequest.cardholderPhotoUrl(),
                    cardholderRequest.cardholderPhotoKey());
            cardholderRepository.save(cardholder);
        }

        return new TravelcardCreateResponseDto(String.valueOf(travelcardId), generateToken());
    }

    private void validateBusinessRules(TravelcardCreateRequestDto request)
    {
        if (request.travelcardRequestedDate().isAfter(java.time.OffsetDateTime.now()))
        {
            throw new IllegalArgumentException("requested date must be in the past");
        }
        if (request.travelcardValidFrom().isAfter(request.travelcardValidTo()))
        {
            throw new IllegalArgumentException("valid_from must not be later than valid_to");
        }
        if (request.travelcardValidTo().isBefore(java.time.OffsetDateTime.now()))
        {
            throw new IllegalArgumentException("valid_to must be in the future");
        }
        if (request.travelcardValidFrom().isAfter(java.time.OffsetDateTime.now().plusMonths(1)))
        {
            throw new IllegalArgumentException("valid_from must be within one calendar month from today");
        }
        if (request.travelcardType() == TravelcardTypeEnum.SixteenToSeventeen && request.travelcardUsableTo() == null)
        {
            throw new IllegalArgumentException("usable_to is required for SixteenToSeventeen");
        }
        if (request.travelcardUsableTo() != null && request.travelcardUsableTo().isBefore(java.time.OffsetDateTime.now()))
        {
            throw new IllegalArgumentException("usable_to must be in the future");
        }
        if ((request.travelcardType() == TravelcardTypeEnum.SixteenToSeventeen || request.travelcardType() == TravelcardTypeEnum.Veterans)
                && request.cardholders().stream().anyMatch(c -> c.cardholderType() == CardholderTypeEnum.Secondary))
        {
            throw new IllegalArgumentException("secondary cardholder is not allowed for this travelcard type");
        }
        long primaryCount = request.cardholders().stream().filter(c -> c.cardholderType() == CardholderTypeEnum.Primary).count();
        if (primaryCount != 1)
        {
            throw new IllegalArgumentException("exactly one primary cardholder is required");
        }
    }

    private String generateToken()
    {
        int value = secureRandom.nextInt(900000) + 100000;
        return String.valueOf(value);
    }
}
