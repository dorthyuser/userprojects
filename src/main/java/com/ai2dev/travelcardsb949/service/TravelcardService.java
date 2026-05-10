package com.ai2dev.travelcardsb949.service;

import com.ai2dev.travelcardsb949.dto.CardholderRequestDto;
import com.ai2dev.travelcardsb949.dto.CreateTravelcardRequestDto;
import com.ai2dev.travelcardsb949.dto.CreateTravelcardResponseDto;
import com.ai2dev.travelcardsb949.model.CardholderEntity;
import com.ai2dev.travelcardsb949.model.CardholderType;
import com.ai2dev.travelcardsb949.model.TravelcardEntity;
import com.ai2dev.travelcardsb949.model.TravelcardType;
import com.ai2dev.travelcardsb949.repository.CardholderRepository;
import com.ai2dev.travelcardsb949.repository.TravelcardRepository;
import java.time.Clock;
import java.time.LocalDateTime;
import java.time.YearMonth;
import java.time.ZoneOffset;
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
    public CreateTravelcardResponseDto createTravelcard(CreateTravelcardRequestDto request)
    {
        logger.info("DB operation table=travelcards operation=INSERT");
        validateBusinessRules(request);
        TravelcardEntity travelcard = new TravelcardEntity(null, request.travelcardType(), request.travelcardValidFrom(), request.travelcardValidTo(), request.travelcardName(), request.travelcardNumber(), request.travelcardRequestedDate(), request.travelcardTransactionReference(), request.travelcardUsableTo());
        TravelcardEntity savedTravelcard = travelcardRepository.save(travelcard);
        logger.info("DB operation table=cardholders operation=INSERT");
        List<CardholderEntity> cardholders = request.cardholders().stream().map(dto -> new CardholderEntity(null, savedTravelcard.id(), dto.cardholderTitle(), dto.cardholderForename(), dto.cardholderSurname(), dto.cardholderType(), dto.cardholderPhotoName(), dto.cardholderPhotoRRSKey(), dto.cardholderPhotoURL(), dto.cardholderPhotoKey())).toList();
        for (CardholderEntity cardholder : cardholders)
        {
            cardholderRepository.save(cardholder);
        }
        String token = UUID.randomUUID().toString().replace("-", "").substring(0, 6).toUpperCase();
        return new CreateTravelcardResponseDto(String.valueOf(savedTravelcard.id()), token);
    }

    private void validateBusinessRules(CreateTravelcardRequestDto request)
    {
        LocalDateTime now = LocalDateTime.now(clock);
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
            throw new IllegalArgumentException("travelcardValidFrom must be within one calendar month from today");
        }
        if (request.travelcardType() == TravelcardType.SixteenToSeventeen && request.travelcardUsableTo() == null)
        {
            throw new IllegalArgumentException("travelcardUsableTo is required for SixteenToSeventeen");
        }
        if (request.travelcardUsableTo() != null && request.travelcardUsableTo().isBefore(now))
        {
            throw new IllegalArgumentException("travelcardUsableTo must be in the future");
        }
        boolean hasSecondary = request.cardholders().stream().anyMatch(cardholder -> cardholder.cardholderType() == CardholderType.Secondary);
        if (hasSecondary && (request.travelcardType() == TravelcardType.SixteenToSeventeen || request.travelcardType() == TravelcardType.Veterans))
        {
            throw new IllegalArgumentException("secondary cardholder is not allowed for this travelcard type");
        }
    }
}