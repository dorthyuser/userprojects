package com.ai2dev.travelcardsb1005.service;

import com.ai2dev.travelcardsb1005.dto.CardholderRequestDto;
import com.ai2dev.travelcardsb1005.dto.TravelcardRequestDto;
import com.ai2dev.travelcardsb1005.dto.TravelcardResponseDto;
import com.ai2dev.travelcardsb1005.model.CardholderEntity;
import com.ai2dev.travelcardsb1005.model.CardholderTypeEnum;
import com.ai2dev.travelcardsb1005.model.TravelcardEntity;
import com.ai2dev.travelcardsb1005.model.TravelcardTypeEnum;
import com.ai2dev.travelcardsb1005.repository.CardholderRepository;
import com.ai2dev.travelcardsb1005.repository.TravelcardRepository;
import java.time.Clock;
import java.time.OffsetDateTime;
import java.time.YearMonth;
import java.util.ArrayList;
import java.util.List;
import java.util.UUID;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

@Service
public class TravelcardService
{
    private static final Logger log = LoggerFactory.getLogger(TravelcardService.class);

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
    public TravelcardResponseDto createTravelcard(String clientId, String correlationId, TravelcardRequestDto request)
    {
        log.info("service=TravelcardService action=create table=travelcards operation=INSERT client_id={} correlation_id={}", clientId, correlationId);
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

        TravelcardEntity saved = travelcardRepository.save(travelcard);
        List<CardholderEntity> savedCardholders = new ArrayList<>();
        for (CardholderRequestDto cardholderRequest : request.cardholders())
        {
            CardholderEntity cardholder = new CardholderEntity(
                null,
                saved.id(),
                cardholderRequest.cardholderTitle(),
                cardholderRequest.cardholderForename(),
                cardholderRequest.cardholderSurname(),
                cardholderRequest.cardholderType(),
                cardholderRequest.cardholderPhotoName(),
                cardholderRequest.cardholderPhotoRRSKey(),
                cardholderRequest.cardholderPhotoURL(),
                cardholderRequest.cardholderPhotoKey());
            savedCardholders.add(cardholderRepository.save(cardholder));
        }

        String token = UUID.randomUUID().toString().substring(0, 6).toUpperCase();
        return new TravelcardResponseDto(String.valueOf(saved.id()), token);
    }

    private void validateBusinessRules(TravelcardRequestDto request)
    {
        OffsetDateTime now = OffsetDateTime.now(clock);
        if (request.travelcardRequestedDate().isAfter(now))
        {
            throw new IllegalArgumentException("travelcardRequestedDate must be in the past");
        }
        if (request.travelcardValidFrom().isAfter(YearMonth.from(now).plusMonths(1).atDay(1).atStartOfDay().atOffset(now.getOffset())))
        {
            throw new IllegalArgumentException("travelcardValidFrom must be within one calendar month from today");
        }
        if (!request.travelcardValidFrom().isBefore(request.travelcardValidTo()))
        {
            throw new IllegalArgumentException("travelcardValidFrom must be earlier than travelcardValidTo");
        }
        if (request.travelcardValidTo().isBefore(now))
        {
            throw new IllegalArgumentException("travelcardValidTo must be in the future");
        }
        if (request.travelcardUsableTo() != null && request.travelcardUsableTo().isBefore(now))
        {
            throw new IllegalArgumentException("travelcardUsableTo must be in the future");
        }
        if (request.travelcardType() == TravelcardTypeEnum.SixteenToSeventeen && request.travelcardUsableTo() == null)
        {
            throw new IllegalArgumentException("travelcardUsableTo is required for SixteenToSeventeen");
        }
        boolean hasSecondary = request.cardholders().stream().anyMatch(cardholder -> cardholder.cardholderType() == CardholderTypeEnum.Secondary);
        if (hasSecondary && (request.travelcardType() == TravelcardTypeEnum.SixteenToSeventeen || request.travelcardType() == TravelcardTypeEnum.Veterans))
        {
            throw new IllegalArgumentException("Secondary cardholder is not allowed for this travelcard type");
        }
    }
}
