package com.ai2dev.travelcardspringboot1057.service;

import com.ai2dev.travelcardspringboot1057.dto.CardholderRequestDto;
import com.ai2dev.travelcardspringboot1057.dto.CreateTravelcardResponseDto;
import com.ai2dev.travelcardspringboot1057.dto.TravelcardRequestDto;
import com.ai2dev.travelcardspringboot1057.model.CardholderEntity;
import com.ai2dev.travelcardspringboot1057.model.CardholderType;
import com.ai2dev.travelcardspringboot1057.model.TravelcardEntity;
import com.ai2dev.travelcardspringboot1057.model.TravelcardType;
import com.ai2dev.travelcardspringboot1057.repository.CardholderRepository;
import com.ai2dev.travelcardspringboot1057.repository.TravelcardRepository;
import java.time.OffsetDateTime;
import java.util.UUID;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

@Service
public class TravelcardService {

    private static final Logger log = LoggerFactory.getLogger(TravelcardService.class);
    private final TravelcardRepository travelcardRepository;
    private final CardholderRepository cardholderRepository;

    public TravelcardService(TravelcardRepository travelcardRepository, CardholderRepository cardholderRepository) {
        this.travelcardRepository = travelcardRepository;
        this.cardholderRepository = cardholderRepository;
    }

    @Transactional
    public CreateTravelcardResponseDto createTravelcard(TravelcardRequestDto request) {
        validateBusinessRules(request);
        log.info("DB operation INSERT table=travelcards");
        TravelcardEntity savedTravelcard = travelcardRepository.save(new TravelcardEntity(
                null,
                request.travelcardType(),
                request.travelcardValidFrom(),
                request.travelcardValidTo(),
                request.travelcardName(),
                request.travelcardNumber(),
                request.travelcardRequestedDate(),
                request.travelcardTransactionReference(),
                request.travelcardUsableTo()
        ));

        for (CardholderRequestDto cardholder : request.cardholders()) {
            log.info("DB operation INSERT table=cardholders");
            cardholderRepository.save(new CardholderEntity(
                    null,
                    savedTravelcard.id(),
                    cardholder.cardholderTitle(),
                    cardholder.cardholderForename(),
                    cardholder.cardholderSurname(),
                    cardholder.cardholderType(),
                    cardholder.cardholderPhotoName(),
                    cardholder.cardholderPhotoRrsKey(),
                    cardholder.cardholderPhotoUrl(),
                    cardholder.cardholderPhotoKey()
            ));
        }

        return new CreateTravelcardResponseDto(String.valueOf(savedTravelcard.id()), generateToken());
    }

    private void validateBusinessRules(TravelcardRequestDto request) {
        OffsetDateTime now = OffsetDateTime.now();
        if (request.travelcardRequestedDate().isAfter(now)) {
            throw new IllegalArgumentException("travelcardRequestedDate must be in the past");
        }
        if (request.travelcardValidFrom().isAfter(now.plusMonths(1))) {
            throw new IllegalArgumentException("travelcardValidFrom must be no later than one calendar month from now");
        }
        if (request.travelcardValidFrom().isAfter(request.travelcardValidTo())) {
            throw new IllegalArgumentException("travelcardValidFrom must not be later than travelcardValidTo");
        }
        if (request.travelcardValidTo().isBefore(now)) {
            throw new IllegalArgumentException("travelcardValidTo must be in the future");
        }
        if (request.travelcardType() == TravelcardType.SixteenToSeventeen && request.travelcardUsableTo() == null) {
            throw new IllegalArgumentException("travelcardUsableTo is required for SixteenToSeventeen");
        }
        if (request.travelcardUsableTo() != null && request.travelcardUsableTo().isBefore(now)) {
            throw new IllegalArgumentException("travelcardUsableTo must be in the future");
        }
        if ((request.travelcardType() == TravelcardType.SixteenToSeventeen || request.travelcardType() == TravelcardType.Veterans)
                && request.cardholders().stream().anyMatch(c -> c.cardholderType() == CardholderType.Secondary)) {
            throw new IllegalArgumentException("Secondary cardholder is not allowed for this travelcard type");
        }
        long primaryCount = request.cardholders().stream().filter(c -> c.cardholderType() == CardholderType.Primary).count();
        if (primaryCount != 1) {
            throw new IllegalArgumentException("Exactly one Primary cardholder is required");
        }
    }

    private String generateToken() {
        return UUID.randomUUID().toString().substring(0, 6).replace("-", "A").toUpperCase();
    }
}
