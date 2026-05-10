package com.ai2dev.travelcardspringboot1248.service;

import com.ai2dev.travelcardspringboot1248.dto.CardholderRequestDto;
import com.ai2dev.travelcardspringboot1248.dto.TravelcardCreateRequestDto;
import com.ai2dev.travelcardspringboot1248.dto.TravelcardCreateResponseDto;
import com.ai2dev.travelcardspringboot1248.model.CardholderEntity;
import com.ai2dev.travelcardspringboot1248.model.CardholderType;
import com.ai2dev.travelcardspringboot1248.model.TravelcardEntity;
import com.ai2dev.travelcardspringboot1248.model.TravelcardType;
import com.ai2dev.travelcardspringboot1248.repository.CardholderRepository;
import com.ai2dev.travelcardspringboot1248.repository.TravelcardRepository;
import java.time.Instant;
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
    private static final Logger log = LoggerFactory.getLogger(TravelcardService.class);

    private final TravelcardRepository travelcardRepository;
    private final CardholderRepository cardholderRepository;

    public TravelcardService(TravelcardRepository travelcardRepository, CardholderRepository cardholderRepository)
    {
        this.travelcardRepository = travelcardRepository;
        this.cardholderRepository = cardholderRepository;
    }

    @Transactional
    public TravelcardCreateResponseDto createTravelcard(TravelcardCreateRequestDto request)
    {
        validateBusinessRules(request);

        log.info("db_operation table=travelcards operation=INSERT");
        TravelcardEntity savedTravelcard = travelcardRepository.save(new TravelcardEntity(
                null,
                request.travelcardType(),
                request.travelcardValidFrom(),
                request.travelcardValidTo(),
                request.travelcardName(),
                request.travelcardNumber(),
                request.travelcardRequestedDate(),
                request.travelcardTransactionReference(),
                request.travelcardUsableTo()));

        for (CardholderRequestDto cardholderRequest : request.cardholders())
        {
            log.info("db_operation table=cardholders operation=INSERT");
            cardholderRepository.save(new CardholderEntity(
                    null,
                    savedTravelcard.id(),
                    cardholderRequest.cardholderTitle(),
                    cardholderRequest.cardholderForename(),
                    cardholderRequest.cardholderSurname(),
                    cardholderRequest.cardholderType(),
                    cardholderRequest.cardholderPhotoName(),
                    cardholderRequest.cardholderPhotoRRSKey(),
                    cardholderRequest.cardholderPhotoURL(),
                    cardholderRequest.cardholderPhotoKey()));
        }

        String token = UUID.randomUUID().toString().replace("-", "").substring(0, 6).toUpperCase();
        log.info("service_exit createTravelcard travelcardId={}", savedTravelcard.id());
        return new TravelcardCreateResponseDto(String.valueOf(savedTravelcard.id()), token);
    }

    private void validateBusinessRules(TravelcardCreateRequestDto request)
    {
        Instant now = Instant.now();

        if (request.travelcardRequestedDate().isAfter(now))
        {
            throw new IllegalArgumentException("travelcardRequestedDate must be in the past");
        }
        if (!request.travelcardValidFrom().isBefore(request.travelcardValidTo()))
        {
            throw new IllegalArgumentException("travelcardValidFrom must be later than travelcardValidTo");
        }
        if (request.travelcardValidTo().isBefore(now))
        {
            throw new IllegalArgumentException("travelcardValidTo must be in the future");
        }
        if (request.travelcardValidFrom().isAfter(now.plus(31, ChronoUnit.DAYS)))
        {
            throw new IllegalArgumentException("travelcardValidFrom must be within one calendar month");
        }
        if (request.travelcardUsableTo() != null && request.travelcardUsableTo().isBefore(now))
        {
            throw new IllegalArgumentException("travelcardUsableTo must be in the future");
        }
        if (request.travelcardType() == TravelcardType.SixteenToSeventeen && request.travelcardUsableTo() == null)
        {
            throw new IllegalArgumentException("travelcardUsableTo is required for SixteenToSeventeen");
        }
        if ((request.travelcardType() == TravelcardType.SixteenToSeventeen || request.travelcardType() == TravelcardType.Veterans)
                && request.cardholders().stream().anyMatch(cardholder -> cardholder.cardholderType() == CardholderType.Secondary))
        {
            throw new IllegalArgumentException("Secondary cardholder is not allowed for this travelcard type");
        }
        long primaryCount = request.cardholders().stream().filter(cardholder -> cardholder.cardholderType() == CardholderType.Primary).count();
        if (primaryCount != 1)
        {
            throw new IllegalArgumentException("Exactly one primary cardholder is required");
        }
    }
}