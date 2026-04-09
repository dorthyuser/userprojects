package com.ai2dev.sptesting.service;

import com.ai2dev.sptesting.dto.CardholderRequest;
import com.ai2dev.sptesting.dto.TravelcardRequest;
import com.ai2dev.sptesting.model.Cardholder;
import com.ai2dev.sptesting.model.CardholderType;
import com.ai2dev.sptesting.model.Travelcard;
import com.ai2dev.sptesting.model.TravelcardType;
import com.ai2dev.sptesting.repository.CardholderRepository;
import com.ai2dev.sptesting.repository.TravelcardRepository;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;
import java.time.Instant;
import java.time.temporal.ChronoUnit;
import java.util.HashSet;
import java.util.List;
import java.util.Set;
import java.util.UUID;

@Service
public class TravelcardService {
    private static final Logger logger = LoggerFactory.getLogger(TravelcardService.class);

    private final CardholderRepository cardholderRepository;
    private final TravelcardRepository travelcardRepository;

    public TravelcardService(CardholderRepository cardholderRepository, TravelcardRepository travelcardRepository) {
        this.cardholderRepository = cardholderRepository;
        this.travelcardRepository = travelcardRepository;
    }

    public String generateToken() {
        logger.debug("Generating token");
        String token = UUID.randomUUID().toString().replaceAll("[^A-Za-z0-9]", "").substring(0, 6).toUpperCase();
        logger.debug("Generated token: {}", token);
        return token;
    }

    public UUID createTravelcard(TravelcardRequest request) {
        logger.info("Enter createTravelcard");

        validateBusinessRules(request);

        Travelcard entity = new Travelcard();
        entity.setTravelcardType(TravelcardType.valueOf(request.travelcardType()));
        entity.setTravelcardValidFrom(request.travelcardValidFrom());
        entity.setTravelcardValidTo(request.travelcardValidTo());
        entity.setTravelcardName(request.travelcardName());
        entity.setTravelcardNumber(request.travelcardNumber());
        entity.setTravelcardRequestedDate(request.travelcardRequestedDate());
        entity.setTravelcardTransactionReference(request.travelcardTransactionReference());
        entity.setTravelcardUsableTo(request.travelcardUsableTo());

        Travelcard saved = travelcardRepository.save(entity);

        List<CardholderRequest> chs = request.cardholders();
        for (CardholderRequest chReq : chs) {
            Cardholder ch = new Cardholder();
            ch.setTravelcardId(saved.getId());
            ch.setCardholderTitle(chReq.cardholderTitle());
            ch.setCardholderForename(chReq.cardholderForename());
            ch.setCardholderSurname(chReq.cardholderSurname());
            ch.setCardholderType(CardholderType.valueOf(chReq.cardholderType()));
            ch.setCardholderPhotoName(chReq.cardholderPhotoName());
            ch.setCardholderPhotoRrsKey(chReq.cardholderPhotoRRSKey());
            ch.setCardholderPhotoUrl(chReq.cardholderPhotoURL());
            ch.setCardholderPhotoKey(chReq.cardholderPhotoKey());
            cardholderRepository.save(ch);
        }

        UUID responseId = UUID.randomUUID();
        logger.info("Exit createTravelcard");
        return responseId;
    }

    private void validateBusinessRules(TravelcardRequest request) {
        logger.debug("Enter validateBusinessRules");

        Instant now = Instant.now().truncatedTo(ChronoUnit.MILLIS);

        if (request.travelcardRequestedDate().isAfter(now)) {
            logger.error("Requested date must be in the past or present");
            throw new IllegalArgumentException("travelcardRequestedDate must be in the past or present");
        }

        if (request.travelcardValidFrom().isAfter(request.travelcardValidTo())) {
            logger.error("Valid from must be before valid to");
            throw new IllegalArgumentException("travelcardValidFrom must be before travelcardValidTo");
        }

        if (request.travelcardValidTo().isBefore(now)) {
            logger.error("Valid to must be in the future");
            throw new IllegalArgumentException("travelcardValidTo must be in the future");
        }

        if ("SixteenToSeventeen".equals(request.travelcardType())) {
            if (request.travelcardUsableTo() == null) {
                logger.error("usableTo is required for SixteenToSeventeen");
                throw new IllegalArgumentException("travelcardUsableTo is required for SixteenToSeventeen type");
            }

            if (request.travelcardUsableTo().isBefore(now)) {
                logger.error("usableTo must be in the future");
                throw new IllegalArgumentException("travelcardUsableTo must be in the future");
            }
        }

        // Check cardholders count and types
        List<CardholderRequest> chs = request.cardholders();
        if (chs.size() < 1 || chs.size() > 2) {
            logger.error("cardholders must be 1 or 2");
            throw new IllegalArgumentException("cardholders must contain exactly one or two items");
        }

        Set<String> types = new HashSet<>();
        int primaryCount = 0;
        int secondaryCount = 0;

        for (CardholderRequest ch : chs) {
            types.add(ch.cardholderType());
            if ("Primary".equals(ch.cardholderType())) {
                primaryCount++;
            } else if ("Secondary".equals(ch.cardholderType())) {
                secondaryCount++;
            }

            int imageCount = 0;
            if (ch.cardholderPhotoRRSKey() != null && !ch.cardholderPhotoRRSKey().isBlank()) imageCount++;
            if (ch.cardholderPhotoURL() != null && !ch.cardholderPhotoURL().isBlank()) imageCount++;
            if (ch.cardholderPhotoKey() != null && !ch.cardholderPhotoKey().isBlank()) imageCount++;

            if (imageCount != 1) {
                logger.error("Cardholder must have exactly one image reference");
                throw new IllegalArgumentException("Each cardholder must have exactly one of cardholderPhotoRRSKey, cardholderPhotoURL, cardholderPhotoKey");
            }
        }

        if (primaryCount != 1) {
            logger.error("Exactly one primary required");
            throw new IllegalArgumentException("Exactly one Primary cardholder is required");
        }

        if (secondaryCount > 1) {
            logger.error("At most one secondary allowed");
            throw new IllegalArgumentException("At most one Secondary cardholder is allowed");
        }

        if (secondaryCount == 1) {
            // Only allow secondary for certain travelcard types
            if (!isSecondaryAllowed(request.travelcardType())) {
                logger.error("Secondary cardholder not allowed for type {}", request.travelcardType());
                throw new IllegalArgumentException("Secondary cardholder not allowed for travelcard type " + request.travelcardType());
            }
        }

        logger.debug("Exit validateBusinessRules");
    }

    private boolean isSecondaryAllowed(String travelcardType) {
        // Business assumption: only TwoTogether and Family allow a secondary cardholder
        return "TwoTogether".equals(travelcardType) || "Family".equals(travelcardType);
    }
}
