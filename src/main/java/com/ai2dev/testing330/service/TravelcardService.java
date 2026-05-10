package com.ai2dev.testing330.service;

import com.ai2dev.testing330.dto.CreateCardholderDto;
import com.ai2dev.testing330.dto.CreateTravelcardRequestDto;
import com.ai2dev.testing330.dto.CreateTravelcardResponseDto;
import com.ai2dev.testing330.model.CardholderEntity;
import com.ai2dev.testing330.model.CardholderTypeEnum;
import com.ai2dev.testing330.model.TravelcardEntity;
import com.ai2dev.testing330.model.TravelcardTypeEnum;
import com.ai2dev.testing330.repository.CardholderRepository;
import com.ai2dev.testing330.repository.TravelcardRepository;
import java.time.OffsetDateTime;
import java.util.List;
import java.util.Objects;
import java.util.concurrent.ThreadLocalRandom;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

@Service
public class TravelcardService
{
    private static final Logger logger = LoggerFactory.getLogger(TravelcardService.class);

    private final CardholderRepository cardholderRepository;
    private final TravelcardRepository travelcardRepository;

    public TravelcardService(CardholderRepository cardholderRepository, TravelcardRepository travelcardRepository)
    {
        this.cardholderRepository = cardholderRepository;
        this.travelcardRepository = travelcardRepository;
    }

    @Transactional
    public CreateTravelcardResponseDto createTravelcard(CreateTravelcardRequestDto request, String clientId, String correlationId)
    {
        logger.info("Business entry createTravelcard client_id={} correlation_id={}", clientId, correlationId);
        validateBusinessRules(request);
        TravelcardEntity travelcardEntity = new TravelcardEntity(
                null,
                request.travelcardType(),
                request.travelcardValidFrom(),
                request.travelcardValidTo(),
                request.travelcardName(),
                request.travelcardNumber(),
                request.travelcardRequestedDate(),
                request.travelcardTransactionReference(),
                request.travelcardUsableTo());
        logger.info("DB operation table=travelcards operation=INSERT");
        TravelcardEntity savedTravelcard = travelcardRepository.save(travelcardEntity);
        Long travelcardId = savedTravelcard.id();
        for (CreateCardholderDto cardholder : request.cardholders())
        {
            CardholderEntity entity = new CardholderEntity(
                    null,
                    travelcardId.intValue(),
                    cardholder.cardholderTitle(),
                    cardholder.cardholderForename(),
                    cardholder.cardholderSurname(),
                    cardholder.cardholderType(),
                    cardholder.cardholderPhotoName(),
                    cardholder.cardholderPhotoRRSKey(),
                    cardholder.cardholderPhotoURL(),
                    cardholder.cardholderPhotoKey());
            logger.info("DB operation table=cardholders operation=INSERT");
            cardholderRepository.save(entity);
        }
        String token = generateToken();
        logger.info("Business exit createTravelcard travelcard_id={}", travelcardId);
        return new CreateTravelcardResponseDto(String.valueOf(travelcardId), token);
    }

    private void validateBusinessRules(CreateTravelcardRequestDto request)
    {
        OffsetDateTime now = OffsetDateTime.now();
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
        if (request.travelcardUsableTo() != null && request.travelcardUsableTo().isBefore(now))
        {
            throw new IllegalArgumentException("travelcardUsableTo must be in the future");
        }
        if (request.travelcardType() != TravelcardTypeEnum.SixteenToSeventeen && request.travelcardUsableTo() != null)
        {
            throw new IllegalArgumentException("travelcardUsableTo is only allowed for SixteenToSeventeen");
        }
        if (request.travelcardType() == TravelcardTypeEnum.SixteenToSeventeen && request.travelcardUsableTo() == null)
        {
            throw new IllegalArgumentException("travelcardUsableTo is required for SixteenToSeventeen");
        }
        long secondaryCount = request.cardholders().stream().filter(cardholder -> cardholder.cardholderType() == CardholderTypeEnum.Secondary).count();
        if (secondaryCount > 1)
        {
            throw new IllegalArgumentException("only one secondary cardholder is allowed");
        }
        if ((request.travelcardType() == TravelcardTypeEnum.SixteenToSeventeen || request.travelcardType() == TravelcardTypeEnum.Veterans) && secondaryCount > 0)
        {
            throw new IllegalArgumentException("secondary cardholder is not allowed for this travelcard type");
        }
        long primaryCount = request.cardholders().stream().filter(cardholder -> cardholder.cardholderType() == CardholderTypeEnum.Primary).count();
        if (primaryCount != 1 || request.cardholders().size() < 1 || request.cardholders().size() > 2)
        {
            throw new IllegalArgumentException("exactly one primary cardholder and at most one secondary cardholder are required");
        }
    }

    private String generateToken()
    {
        int value = ThreadLocalRandom.current().nextInt(100000, 1000000);
        return "P" + value;
    }
}