package com.ai2dev.travelcardsplambda425.service;

import com.ai2dev.travelcardsplambda425.model.Cardholder;
import com.ai2dev.travelcardsplambda425.model.CardholderRequest;
import com.ai2dev.travelcardsplambda425.model.CardholderType;
import com.ai2dev.travelcardsplambda425.model.CreateTravelcardRequest;
import com.ai2dev.travelcardsplambda425.model.CreateTravelcardResponse;
import com.ai2dev.travelcardsplambda425.model.Travelcard;
import com.ai2dev.travelcardsplambda425.model.TravelcardType;
import com.ai2dev.travelcardsplambda425.repository.CardholderRepository;
import com.ai2dev.travelcardsplambda425.repository.TravelcardRepository;
import java.time.OffsetDateTime;
import java.util.ArrayList;
import java.util.List;
import java.util.UUID;
import java.util.concurrent.ThreadLocalRandom;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.cache.annotation.Cacheable;
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
    @Cacheable(cacheNames = "travelcardCreateResponses")
    public CreateTravelcardResponse create(CreateTravelcardRequest request) {
        log.info("Entering TravelcardService.create");
        validate(request);

        Travelcard travelcard = new Travelcard(null, request.travelcardType(), request.travelcardValidFrom(), request.travelcardValidTo(), request.travelcardName() != null ? request.travelcardName() : request.travelcardType().name(), request.travelcardNumber(), request.travelcardRequestedDate(), request.travelcardTransactionReference(), request.travelcardUsableTo());
        Travelcard savedTravelcard = travelcardRepository.save(travelcard);

        List<Cardholder> entities = new ArrayList<>();
        for (CardholderRequest cardholderRequest : request.cardholders()) {
            entities.add(new Cardholder(null, savedTravelcard.getId(), cardholderRequest.cardholderTitle(), cardholderRequest.cardholderForename(), cardholderRequest.cardholderSurname(), cardholderRequest.cardholderType(), cardholderRequest.cardholderPhotoName(), cardholderRequest.cardholderPhotoRRSKey(), cardholderRequest.cardholderPhotoURL(), cardholderRequest.cardholderPhotoKey()));
        }
        cardholderRepository.saveAll(entities);

        String token = generateToken();
        CreateTravelcardResponse response = new CreateTravelcardResponse(String.valueOf(savedTravelcard.getId()), token);
        log.info("Exiting TravelcardService.create");
        return response;
    }

    private void validate(CreateTravelcardRequest request) {
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
            throw new IllegalArgumentException("travelcardValidFrom must not be later than one calendar month from today");
        }
        if (request.travelcardType() == TravelcardType.SixteenToSeventeen && request.travelcardUsableTo() == null) {
            throw new IllegalArgumentException("travelcardUsableTo is required for SixteenToSeventeen");
        }
        if (request.travelcardUsableTo() != null && request.travelcardUsableTo().isBefore(now)) {
            throw new IllegalArgumentException("travelcardUsableTo must be in the future");
        }
        if ((request.travelcardType() == TravelcardType.SixteenToSeventeen || request.travelcardType() == TravelcardType.Veterans) && hasSecondary(request.cardholders())) {
            throw new IllegalArgumentException("Secondary cardholder is not allowed for this travelcard type");
        }
        if (request.cardholders() == null || request.cardholders().isEmpty() || request.cardholders().size() > 2) {
            throw new IllegalArgumentException("cardholders must contain exactly one or two items");
        }
        long primaryCount = request.cardholders().stream().filter(c -> c.cardholderType() == CardholderType.Primary).count();
        if (primaryCount != 1) {
            throw new IllegalArgumentException("Exactly one primary cardholder is required");
        }
    }

    private boolean hasSecondary(List<CardholderRequest> cardholders) {
        return cardholders.stream().anyMatch(c -> c.cardholderType() == CardholderType.Secondary);
    }

    private String generateToken() {
        int value = ThreadLocalRandom.current().nextInt(1000000);
        return String.format("%06d", value).substring(0, 6);
    }
}