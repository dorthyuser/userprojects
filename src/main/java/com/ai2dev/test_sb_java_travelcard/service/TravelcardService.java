package com.ai2dev.test_sb_java_travelcard.service;

import com.ai2dev.test_sb_java_travelcard.dto.CardholderDto;
import com.ai2dev.test_sb_java_travelcard.dto.TravelcardRequest;
import com.ai2dev.test_sb_java_travelcard.dto.TravelcardResponse;
import com.ai2dev.test_sb_java_travelcard.model.Cardholder;
import com.ai2dev.test_sb_java_travelcard.model.CardholderType;
import com.ai2dev.test_sb_java_travelcard.model.Travelcard;
import com.ai2dev.test_sb_java_travelcard.model.TravelcardType;
import com.ai2dev.test_sb_java_travelcard.repository.CardholderRepository;
import com.ai2dev.test_sb_java_travelcard.repository.TravelcardRepository;
import java.time.OffsetDateTime;
import java.util.List;
import java.util.Objects;
import java.util.Random;
import java.util.UUID;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

@Service
public class TravelcardService {
    private static final Logger logger = LoggerFactory.getLogger(TravelcardService.class);

    private final TravelcardRepository travelcardRepository;
    private final CardholderRepository cardholderRepository;

    public TravelcardService(TravelcardRepository travelcardRepository, CardholderRepository cardholderRepository) {
        this.travelcardRepository = travelcardRepository;
        this.cardholderRepository = cardholderRepository;
    }

    @Transactional
    public TravelcardResponse createTravelcard(TravelcardRequest req) {
        logger.info("Enter createTravelcard request={}", req);

        validateBusinessRules(req);

        TravelcardType tcType;
        try {
            tcType = TravelcardType.valueOf(req.travelcardType());
        } catch (Exception e) {
            logger.error("Invalid travelcardType", e);
            throw new IllegalArgumentException("Invalid travelcardType");
        }

        Travelcard tc = new Travelcard(
            null,
            tcType,
            req.travelcardValidFrom(),
            req.travelcardValidTo(),
            req.travelcardName(),
            req.travelcardNumber(),
            req.travelcardRequestedDate(),
            req.travelcardTransactionReference(),
            req.travelcardUsableTo()
        );

        Travelcard saved = travelcardRepository.save(tc);

        List<CardholderDto> chDtos = req.cardholders();
        for (CardholderDto dto : chDtos) {
            CardholderType cht;
            try {
                cht = CardholderType.valueOf(dto.cardholderType());
            } catch (Exception e) {
                logger.error("Invalid cardholderType", e);
                throw new IllegalArgumentException("Invalid cardholderType");
            }

            Cardholder ch = new Cardholder(
                null,
                saved.getId(),
                dto.cardholderTitle(),
                dto.cardholderForename(),
                dto.cardholderSurname(),
                cht,
                dto.cardholderPhotoName(),
                dto.cardholderPhotoRrsKey(),
                dto.cardholderPhotoUrl(),
                dto.cardholderPhotoKey()
            );

            cardholderRepository.save(ch);
        }

        String travelcardId = UUID.randomUUID().toString();
        String token = generateToken(6);

        TravelcardResponse resp = new TravelcardResponse(travelcardId, token);
        logger.info("Exit createTravelcard travelcardId={}", travelcardId);
        return resp;
    }

    private void validateBusinessRules(TravelcardRequest req) {
        logger.info("Enter validateBusinessRules");

        OffsetDateTime now = OffsetDateTime.now();

        if (!req.travelcardRequestedDate().isBefore(now)) {
            throw new IllegalArgumentException("travelcardRequestedDate must be in the past");
        }

        if (req.travelcardValidFrom().isAfter(req.travelcardValidTo())) {
            throw new IllegalArgumentException("travelcardValidFrom must not be later than travelcardValidTo");
        }

        if (!req.travelcardValidTo().isAfter(now)) {
            throw new IllegalArgumentException("travelcardValidTo must be in the future");
        }

        if ("SixteenToSeventeen".equals(req.travelcardType())) {
            if (req.travelcardUsableTo() == null) {
                throw new IllegalArgumentException("travelcardUsableTo is required for SixteenToSeventeen");
            }
            if (!req.travelcardUsableTo().isAfter(now)) {
                throw new IllegalArgumentException("travelcardUsableTo must be in the future");
            }
        }

        List<CardholderDto> ch = req.cardholders();
        if (ch == null || ch.isEmpty() || ch.size() > 2) {
            throw new IllegalArgumentException("cardholders must contain 1 or 2 items");
        }

        long primaryCount = ch.stream().filter(c -> "Primary".equals(c.cardholderType())).count();
        if (primaryCount != 1) {
            throw new IllegalArgumentException("Exactly one Primary cardholder is required");
        }

        long secondaryCount = ch.stream().filter(c -> "Secondary".equals(c.cardholderType())).count();
        if (secondaryCount > 1) {
            throw new IllegalArgumentException("At most one Secondary cardholder allowed");
        }

        if (secondaryCount == 1) {
            // Only allow secondary for specific travelcard types
            if (!("TwoTogether".equals(req.travelcardType()) || "Family".equals(req.travelcardType()))) {
                throw new IllegalArgumentException("Secondary cardholder not allowed for this travelcardType");
            }
        }

        for (CardholderDto c : ch) {
            boolean hasPhotoDetail = (c.cardholderPhotoRrsKey() != null && !c.cardholderPhotoRrsKey().isBlank())
                || (c.cardholderPhotoUrl() != null && !c.cardholderPhotoUrl().isBlank())
                || (c.cardholderPhotoKey() != null && !c.cardholderPhotoKey().isBlank());

            if (!hasPhotoDetail) {
                throw new IllegalArgumentException("Each cardholder must have one of cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key");
            }
        }

        logger.info("Exit validateBusinessRules");
    }

    private String generateToken(int length) {
        String chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        Random rnd = new Random();
        StringBuilder sb = new StringBuilder(length);
        for (int i = 0; i < length; i++) {
            sb.append(chars.charAt(rnd.nextInt(chars.length())));
        }
        return sb.toString();
    }
}
