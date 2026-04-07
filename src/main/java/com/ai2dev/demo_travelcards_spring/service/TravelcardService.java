package com.ai2dev.demo_travelcards_spring.service;

import com.ai2dev.demo_travelcards_spring.dto.CreateTravelcardRequest;
import com.ai2dev.demo_travelcards_spring.model.Cardholder;
import com.ai2dev.demo_travelcards_spring.model.CardholderType;
import com.ai2dev.demo_travelcards_spring.model.Travelcard;
import com.ai2dev.demo_travelcards_spring.model.TravelcardType;
import com.ai2dev.demo_travelcards_spring.repository.CardholderRepository;
import com.ai2dev.demo_travelcards_spring.repository.TravelcardRepository;
import java.time.OffsetDateTime;
import java.util.HashMap;
import java.util.HashSet;
import java.util.List;
import java.util.Map;
import java.util.Set;
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
    public Map<String, Object> create(CreateTravelcardRequest req) {
        log.info("Entry TravelcardService.create");

        validateBusinessRules(req);

        Travelcard travelcard = new Travelcard();
        travelcard.setTravelcardType(req.travelcardType());
        travelcard.setTravelcardValidFrom(req.travelcardValidFrom());
        travelcard.setTravelcardValidTo(req.travelcardValidTo());
        travelcard.setTravelcardName(req.travelcardName());
        travelcard.setTravelcardNumber(req.travelcardNumber());
        travelcard.setTravelcardRequestedDate(req.travelcardRequestedDate());
        travelcard.setTravelcardTransactionReference(req.travelcardTransactionReference());
        travelcard.setTravelcardUsableTo(req.travelcardUsableTo());

        Set<Cardholder> cardholders = new HashSet<>();
        for (CreateTravelcardRequest.CardholderRequest ch : req.cardholders()) {
            Cardholder c = new Cardholder();
            c.setCardholderTitle(ch.cardholderTitle());
            c.setCardholderForename(ch.cardholderForename());
            c.setCardholderSurname(ch.cardholderSurname());
            c.setCardholderType(ch.cardholderType());
            c.setCardholderPhotoName(ch.cardholderPhotoName());
            c.setCardholderPhotoUrl(ch.cardholderPhotoURL());
            c.setCardholderPhotoKey(ch.cardholderPhotoKey());
            cardholders.add(c);
        }

        travelcard.setCardholders(cardholders);

        Travelcard saved = travelcardRepository.save(travelcard);

        // Persist cardholders with travelcard id
        for (Cardholder c : saved.getCardholders()) {
            c.setTravelcardId(saved.getId());
            cardholderRepository.save(c);
        }

        String token = generateToken();

        Map<String, Object> resp = new HashMap<>();
        resp.put("travelcardId", saved.getId().toString());
        resp.put("token", token);

        log.info("Exit TravelcardService.create travelcardId={}", saved.getId());
        return resp;
    }

    private void validateBusinessRules(CreateTravelcardRequest req) {
        OffsetDateTime now = OffsetDateTime.now();

        // requested_date is in the past
        if (req.travelcardRequestedDate().isAfter(now)) {
            log.error("travelcardRequestedDate must be in the past");
            throw new IllegalArgumentException("travelcardRequestedDate must be in the past");
        }

        // valid_from must be before valid_to
        if (!req.travelcardValidFrom().isBefore(req.travelcardValidTo())) {
            log.error("travelcardValidFrom must be before travelcardValidTo");
            throw new IllegalArgumentException("travelcardValidFrom must be before travelcardValidTo");
        }

        // valid_to must be in the future
        if (!req.travelcardValidTo().isAfter(now)) {
            log.error("travelcardValidTo must be in the future");
            throw new IllegalArgumentException("travelcardValidTo must be in the future");
        }

        // usable_to provided if type is SixteenToSeventeen and must be in the future
        if (req.travelcardType() == TravelcardType.SixteenToSeventeen) {
            if (req.travelcardUsableTo() == null) {
                log.error("travelcardUsableTo is required for SixteenToSeventeen travelcards");
                throw new IllegalArgumentException("travelcardUsableTo is required for SixteenToSeventeen travelcards");
            }

            if (!req.travelcardUsableTo().isAfter(now)) {
                log.error("travelcardUsableTo must be in the future");
                throw new IllegalArgumentException("travelcardUsableTo must be in the future");
            }
        } else {
            if (req.travelcardUsableTo() != null) {
                log.error("travelcardUsableTo must not be provided unless travelcardType is SixteenToSeventeen");
                throw new IllegalArgumentException("travelcardUsableTo must not be provided unless travelcardType is SixteenToSeventeen");
            }
        }

        // Check cardholders count and types
        List<CreateTravelcardRequest.CardholderRequest> chs = req.cardholders();
        if (chs.size() < 1 || chs.size() > 2) {
            log.error("cardholders must contain 1 or 2 items");
            throw new IllegalArgumentException("cardholders must contain 1 or 2 items");
        }

        int primaryCount = 0;
        int secondaryCount = 0;
        for (CreateTravelcardRequest.CardholderRequest ch : chs) {
            if (ch.cardholderType() == CardholderType.Primary) {
                primaryCount++;
            } else if (ch.cardholderType() == CardholderType.Secondary) {
                secondaryCount++;
            }

            // Each cardholder requires one of photo URL or photo key
            if ((ch.cardholderPhotoURL() == null || ch.cardholderPhotoURL().isBlank()) && (ch.cardholderPhotoKey() == null || ch.cardholderPhotoKey().isBlank())) {
                log.error("Each cardholder must provide either cardholderPhotoURL or cardholderPhotoKey");
                throw new IllegalArgumentException("Each cardholder must provide either cardholderPhotoURL or cardholderPhotoKey");
            }

            if (ch.cardholderPhotoURL() != null && ch.cardholderPhotoKey() != null) {
                log.error("Provide only one of cardholderPhotoURL or cardholderPhotoKey");
                throw new IllegalArgumentException("Provide only one of cardholderPhotoURL or cardholderPhotoKey");
            }
        }

        if (primaryCount != 1) {
            log.error("Exactly one Primary cardholder is required");
            throw new IllegalArgumentException("Exactly one Primary cardholder is required");
        }

        // Secondary allowed only for specific travelcard types (business rule)
        if (secondaryCount > 0) {
            if (!(req.travelcardType() == TravelcardType.TwoTogether || req.travelcardType() == TravelcardType.Family)) {
                log.error("Secondary cardholder is not allowed for travelcard type {}", req.travelcardType());
                throw new IllegalArgumentException("Secondary cardholder is not allowed for this travelcard type");
            }
        }

        // valid_from must not be later than one calendar month from creation
        OffsetDateTime requested = req.travelcardRequestedDate();
        OffsetDateTime maxValidFrom = requested.plusMonths(1);
        if (req.travelcardValidFrom().isAfter(maxValidFrom)) {
            log.error("travelcardValidFrom must be no later than one calendar month from travelcardRequestedDate");
            throw new IllegalArgumentException("travelcardValidFrom must be no later than one calendar month from travelcardRequestedDate");
        }
    }

    private String generateToken() {
        // Simple token generation: 6 uppercase alphanumeric characters
        String uuid = UUID.randomUUID().toString().replaceAll("[^A-Za-z0-9]", "");
        return uuid.substring(0, Math.min(6, uuid.length())).toUpperCase();
    }
}
