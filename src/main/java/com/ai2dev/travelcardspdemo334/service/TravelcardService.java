package com.ai2dev.travelcardspdemo334.service;

import com.ai2dev.travelcardspdemo334.dto.CardholderRequestDto;
import com.ai2dev.travelcardspdemo334.dto.CreateTravelcardRequestDto;
import com.ai2dev.travelcardspdemo334.dto.CreateTravelcardResponseDto;
import com.ai2dev.travelcardspdemo334.exception.BadRequestException;
import com.ai2dev.travelcardspdemo334.model.CardholderEntity;
import com.ai2dev.travelcardspdemo334.model.CardholderType;
import com.ai2dev.travelcardspdemo334.model.TravelcardEntity;
import com.ai2dev.travelcardspdemo334.model.TravelcardType;
import com.ai2dev.travelcardspdemo334.repository.CardholderRepository;
import com.ai2dev.travelcardspdemo334.repository.TravelcardRepository;
import java.time.Clock;
import java.time.OffsetDateTime;
import java.time.temporal.ChronoUnit;
import java.util.List;
import java.util.UUID;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

@Service
public class TravelcardService
{
    private static final Logger logger = LoggerFactory.getLogger(TravelcardService.class);

    private final CardholderRepository cardholderRepository;
    private final Clock clock;
    private final TravelcardRepository travelcardRepository;

    public TravelcardService(TravelcardRepository travelcardRepository, CardholderRepository cardholderRepository)
    {
        this.travelcardRepository = travelcardRepository;
        this.cardholderRepository = cardholderRepository;
        this.clock = Clock.systemUTC();
    }

    @Transactional
    public CreateTravelcardResponseDto createTravelcard(CreateTravelcardRequestDto request, String clientId, String correlationId)
    {
        logger.info("Business entry table=travelcards operation=INSERT client_id={} correlation_id={}", clientId, correlationId);
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

        TravelcardEntity savedTravelcard = travelcardRepository.save(travelcardEntity);
        logger.info("DB operation table=travelcards operation=INSERT client_id={} correlation_id={}", clientId, correlationId);

        for (CardholderRequestDto cardholder : request.cardholders())
        {
            CardholderEntity cardholderEntity = new CardholderEntity(
                    null,
                    savedTravelcard.id(),
                    cardholder.cardholderTitle(),
                    cardholder.cardholderForename(),
                    cardholder.cardholderSurname(),
                    cardholder.cardholderType(),
                    cardholder.cardholderPhotoName(),
                    cardholder.cardholderPhotoRRSKey(),
                    cardholder.cardholderPhotoURL(),
                    cardholder.cardholderPhotoKey());
            cardholderRepository.save(cardholderEntity);
        }
        logger.info("DB operation table=cardholders operation=INSERT client_id={} correlation_id={}", clientId, correlationId);

        String travelcardId = String.valueOf(savedTravelcard.id());
        String token = UUID.randomUUID().toString().replace("-", "").substring(0, 6).toUpperCase();
        logger.info("Business exit table=travelcards operation=INSERT client_id={} correlation_id={}", clientId, correlationId);
        return new CreateTravelcardResponseDto(travelcardId, token);
    }

    private void validateBusinessRules(CreateTravelcardRequestDto request)
    {
        OffsetDateTime now = OffsetDateTime.now(clock);
        if (request.travelcardRequestedDate().isAfter(now))
        {
            throw new BadRequestException("travelcardRequestedDate must be in the past");
        }
        if (request.travelcardValidFrom().isAfter(request.travelcardValidTo()))
        {
            throw new BadRequestException("travelcardValidFrom must not be later than travelcardValidTo");
        }
        if (request.travelcardValidTo().isBefore(now))
        {
            throw new BadRequestException("travelcardValidTo must be in the future");
        }
        if (request.travelcardValidFrom().isAfter(now.plus(1, ChronoUnit.MONTHS)))
        {
            throw new BadRequestException("travelcardValidFrom must be no later than one calendar month from today");
        }
        if (request.travelcardType() == TravelcardType.SixteenToSeventeen && request.travelcardUsableTo() == null)
        {
            throw new BadRequestException("travelcardUsableTo is required for SixteenToSeventeen travelcard type");
        }
        if (request.travelcardUsableTo() != null && request.travelcardUsableTo().isBefore(now))
        {
            throw new BadRequestException("travelcardUsableTo must be in the future");
        }

        boolean hasSecondary = request.cardholders().stream().anyMatch(cardholder -> cardholder.cardholderType() == CardholderType.Secondary);
        if (hasSecondary && (request.travelcardType() == TravelcardType.SixteenToSeventeen || request.travelcardType() == TravelcardType.Veterans))
        {
            throw new BadRequestException("Secondary cardholder is not allowed for this travelcard type");
        }

        long primaryCount = request.cardholders().stream().filter(cardholder -> cardholder.cardholderType() == CardholderType.Primary).count();
        if (primaryCount != 1)
        {
            throw new BadRequestException("Exactly one Primary cardholder is required");
        }
    }
}
