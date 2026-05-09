package com.ai2dev.travelcardspringboot1202.service;

import com.ai2dev.travelcardspringboot1202.dto.CardholderRequestDto;
import com.ai2dev.travelcardspringboot1202.dto.CreateTravelcardRequestDto;
import com.ai2dev.travelcardspringboot1202.dto.CreateTravelcardResponseDto;
import com.ai2dev.travelcardspringboot1202.model.CardholderEntity;
import com.ai2dev.travelcardspringboot1202.model.CardholderType;
import com.ai2dev.travelcardspringboot1202.model.TravelcardEntity;
import com.ai2dev.travelcardspringboot1202.model.TravelcardType;
import com.ai2dev.travelcardspringboot1202.repository.CardholderRepository;
import com.ai2dev.travelcardspringboot1202.repository.TravelcardRepository;
import java.time.OffsetDateTime;
import java.util.ArrayList;
import java.util.List;
import java.util.Random;
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
    public CreateTravelcardResponseDto createTravelcard(CreateTravelcardRequestDto request, String clientId, String correlationId) {
        log.info("Service entry createTravelcard client_id={} correlation_id={}", clientId, correlationId);
        validateBusinessRules(request);
        TravelcardEntity travelcard = new TravelcardEntity(null, request.travelcardType(), request.travelcardValidFrom(), request.travelcardValidTo(), request.travelcardName(), request.travelcardNumber(), request.travelcardRequestedDate(), request.travelcardTransactionReference(), request.travelcardUsableTo());
        log.info("DB operation table=travelcards operation=INSERT");
        TravelcardEntity saved = travelcardRepository.save(travelcard);
        List<CardholderEntity> cardholders = new ArrayList<>();
        for (CardholderRequestDto cardholderRequest : request.cardholders()) {
            cardholders.add(new CardholderEntity(null, saved.id(), cardholderRequest.cardholderTitle(), cardholderRequest.cardholderForename(), cardholderRequest.cardholderSurname(), cardholderRequest.cardholderType(), cardholderRequest.cardholderPhotoName(), cardholderRequest.cardholderPhotoRrsKey(), cardholderRequest.cardholderPhotoUrl(), cardholderRequest.cardholderPhotoKey()));
        }
        log.info("DB operation table=cardholders operation=INSERT");
        cardholderRepository.saveAll(cardholders);
        String token = generateToken();
        CreateTravelcardResponseDto response = new CreateTravelcardResponseDto(saved.id() == null ? UUID.randomUUID().toString() : UUID.randomUUID().toString(), token);
        log.info("Service exit createTravelcard");
        return response;
    }

    private void validateBusinessRules(CreateTravelcardRequestDto request) {
        OffsetDateTime now = OffsetDateTime.now();
        if (request.travelcardRequestedDate().isAfter(now)) {
            throw new IllegalArgumentException("travelcardRequestedDate must be in the past");
        }
        if (request.travelcardValidFrom().isAfter(request.travelcardValidTo())) {
            throw new IllegalArgumentException("travelcardValidFrom must not be later than travelcardValidTo");
        }
        if (request.travelcardValidTo().isBefore(now)) {
            throw new IllegalArgumentException("travelcardValidTo must be in the future");
        }
        if (request.travelcardValidFrom().isAfter(now.plusMonths(1))) {
            throw new IllegalArgumentException("travelcardValidFrom must be within one calendar month from today");
        }
        if (request.travelcardType() == TravelcardType.SixteenToSeventeen && request.travelcardUsableTo() == null) {
            throw new IllegalArgumentException("travelcardUsableTo is required for SixteenToSeventeen");
        }
        if (request.travelcardUsableTo() != null && request.travelcardUsableTo().isBefore(now)) {
            throw new IllegalArgumentException("travelcardUsableTo must be in the future");
        }
        boolean hasSecondary = request.cardholders().stream().anyMatch(c -> c.cardholderType() == CardholderType.Secondary);
        if (hasSecondary && (request.travelcardType() == TravelcardType.SixteenToSeventeen || request.travelcardType() == TravelcardType.Veterans)) {
            throw new IllegalArgumentException("secondary cardholder is not allowed for this travelcard type");
        }
        long primaryCount = request.cardholders().stream().filter(c -> c.cardholderType() == CardholderType.Primary).count();
        if (primaryCount != 1) {
            throw new IllegalArgumentException("exactly one primary cardholder is required");
        }
    }

    private String generateToken() {
        int value = new Random().nextInt(900000) + 100000;
        return String.valueOf(value);
    }
}
