package com.ai2dev.demo_travelcards_spring.service;

import com.ai2dev.demo_travelcards_spring.dto.CardholderRequest;
import com.ai2dev.demo_travelcards_spring.dto.TravelcardRequest;
import com.ai2dev.demo_travelcards_spring.dto.TravelcardResponse;
import com.ai2dev.demo_travelcards_spring.model.Cardholder;
import com.ai2dev.demo_travelcards_spring.model.Travelcard;
import com.ai2dev.demo_travelcards_spring.model.TravelcardType;
import com.ai2dev.demo_travelcards_spring.repository.CardholderRepository;
import com.ai2dev.demo_travelcards_spring.repository.TravelcardRepository;
import jakarta.validation.ValidationException;
import java.security.SecureRandom;
import java.time.OffsetDateTime;
import java.time.ZoneOffset;
import java.util.List;
import java.util.Locale;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

@Service
public class TravelcardService {
    private static final Logger log = LoggerFactory.getLogger(TravelcardService.class);
    private final TravelcardRepository travelcardRepository;
    private final CardholderRepository cardholderRepository;
    private final SecureRandom random = new SecureRandom();

    public TravelcardService(TravelcardRepository travelcardRepository, CardholderRepository cardholderRepository) {
        this.travelcardRepository = travelcardRepository;
        this.cardholderRepository = cardholderRepository;
    }

    @Transactional
    public TravelcardResponse createTravelcard(TravelcardRequest req) {
        log.info("Enter createTravelcard");

        OffsetDateTime now = OffsetDateTime.now(ZoneOffset.UTC);

        if (!req.travelcardRequestedDate().isBefore(now)) {
            log.error("Requested date must be in the past");
            throw new ValidationException("travelcardRequestedDate must be in the past");
        }

        if (req.travelcardValidFrom().isAfter(req.travelcardValidTo())) {
            log.error("travelcardValidFrom is after travelcardValidTo");
            throw new ValidationException("travelcardValidFrom must not be after travelcardValidTo");
        }

        if (!req.travelcardValidTo().isAfter(now)) {
            log.error("travelcardValidTo must be in the future");
            throw new ValidationException("travelcardValidTo must be in the future");
        }

        TravelcardType type = req.travelcardType();

        if (type == TravelcardType.SixteenToSeventeen) {
            if (req.travelcardUsableTo() == null) {
                log.error("travelcardUsableTo required for SixteenToSeventeen");
                throw new ValidationException("travelcardUsableTo is required for SixteenToSeventeen");
            }

            if (!req.travelcardUsableTo().isAfter(now)) {
                log.error("travelcardUsableTo must be in the future");
                throw new ValidationException("travelcardUsableTo must be in the future");
            }
        } else {
            if (req.travelcardUsableTo() != null) {
                log.error("travelcardUsableTo must be null unless SixteenToSeventeen");
                throw new ValidationException("travelcardUsableTo must be provided only for SixteenToSeventeen");
            }
        }

        List<CardholderRequest> holders = req.cardholders();

        if (holders == null || holders.isEmpty() || holders.size() > 2) {
            log.error("Invalid cardholders count");
            throw new ValidationException("cardholders must contain 1 or 2 items");
        }

        long primaryCount = holders.stream().filter(h -> h.cardholderType().name().equals("Primary")).count();
        long secondaryCount = holders.stream().filter(h -> h.cardholderType().name().equals("Secondary")).count();

        if (primaryCount != 1) {
            log.error("There must be exactly one Primary cardholder");
            throw new ValidationException("There must be exactly one Primary cardholder");
        }

        if (secondaryCount > 1) {
            log.error("At most one Secondary cardholder allowed");
            throw new ValidationException("At most one Secondary cardholder allowed");
        }

        Travelcard travelcard = new Travelcard();
        travelcard.setTravelcardType(type);
        travelcard.setTravelcardValidFrom(req.travelcardValidFrom());
        travelcard.setTravelcardValidTo(req.travelcardValidTo());
        travelcard.setTravelcardName(req.travelcardName());
        travelcard.setTravelcardNumber(req.travelcardNumber());
        travelcard.setTravelcardRequestedDate(req.travelcardRequestedDate());
        travelcard.setTravelcardTransactionReference(req.travelcardTransactionReference());
        travelcard.setTravelcardUsableTo(req.travelcardUsableTo());

        Travelcard saved = travelcardRepository.save(travelcard);

        for (CardholderRequest h : holders) {
            Cardholder ch = new Cardholder();
            ch.setTravelcardId(saved.getId());
            ch.setCardholderTitle(h.cardholderTitle());
            ch.setCardholderForename(h.cardholderForename());
            ch.setCardholderSurname(h.cardholderSurname());
            ch.setCardholderType(h.cardholderType());
            ch.setCardholderPhotoName(h.cardholderPhotoName());
            ch.setCardholderPhotoUrl(h.cardholderPhotoURL());
            ch.setCardholderPhotoKey(h.cardholderPhotoKey());
            ch.setCardholderPhotoRrsKey(h.cardholderPhotoKey());
            cardholderRepository.save(ch);
        }

        String token = generateToken(6);

        TravelcardResponse response = new TravelcardResponse(String.valueOf(saved.getId()), token);

        log.info("Exit createTravelcard travelcardId={}", response.travelcardId());

        return response;
    }

    private String generateToken(int length) {
        String chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        StringBuilder sb = new StringBuilder(length);

        for (int i = 0; i < length; i++) {
            int idx = random.nextInt(chars.length());
            sb.append(chars.charAt(idx));
        }

        return sb.toString();
    }
}
