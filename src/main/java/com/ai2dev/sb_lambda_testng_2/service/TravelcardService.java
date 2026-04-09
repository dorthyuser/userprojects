package com.ai2dev.sb_lambda_testng_2.service;

import com.ai2dev.sb_lambda_testng_2.dto.CardholderRequest;
import com.ai2dev.sb_lambda_testng_2.dto.TravelcardRequest;
import com.ai2dev.sb_lambda_testng_2.dto.TravelcardResponse;
import com.ai2dev.sb_lambda_testng_2.model.Cardholder;
import com.ai2dev.sb_lambda_testng_2.model.CardholderType;
import com.ai2dev.sb_lambda_testng_2.model.Travelcard;
import com.ai2dev.sb_lambda_testng_2.model.TravelcardType;
import com.ai2dev.sb_lambda_testng_2.repository.CardholderRepository;
import com.ai2dev.sb_lambda_testng_2.repository.TravelcardRepository;
import java.time.OffsetDateTime;
import java.util.List;
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
    public TravelcardResponse createTravelcard(TravelcardRequest req, String clientId, String correlationId) {
        log.info("Enter createTravelcard clientId={} correlationId={}", clientId, correlationId);

        OffsetDateTime now = OffsetDateTime.now();

        if (req.travelcardRequestedDate().isAfter(now)) {
            throw new IllegalArgumentException("requested_date must be in the past");
        }

        if (req.travelcardValidFrom().isAfter(req.travelcardValidTo())) {
            throw new IllegalArgumentException("valid_from must be before or equal to valid_to");
        }

        if (!req.travelcardValidTo().isAfter(now)) {
            throw new IllegalArgumentException("valid_to must be in the future");
        }

        if (req.travelcardType() == TravelcardType.SixteenToSeventeen) {
            if (req.travelcardUsableTo() == null) {
                throw new IllegalArgumentException("usable_to is required for SixteenToSeventeen type");
            }
            if (!req.travelcardUsableTo().isAfter(now)) {
                throw new IllegalArgumentException("usable_to must be in the future");
            }
        }

        List<CardholderRequest> chList = req.cardholders();
        if (chList == null || chList.isEmpty() || chList.size() > 2) {
            throw new IllegalArgumentException("cardholders must contain 1 or 2 items");
        }

        long secondaryCount = chList.stream().filter(c -> c.cardholderType() == CardholderType.Secondary).count();
        if (secondaryCount > 1) {
            throw new IllegalArgumentException("Only one secondary allowed");
        }

        boolean hasSecondary = secondaryCount == 1;
        boolean secondaryAllowed = allowsSecondary(req.travelcardType());
        if (hasSecondary && !secondaryAllowed) {
            throw new IllegalArgumentException("Secondary cardholder not allowed for this travelcard type");
        }

        for (CardholderRequest ch : chList) {
            boolean anyPhoto = ch.cardholderPhotoRRSKey() != null || ch.cardholderPhotoURL() != null || ch.cardholderPhotoKey() != null;
            if (!anyPhoto) {
                throw new IllegalArgumentException("Each cardholder must have one type of image detail");
            }
        }

        Travelcard t = new Travelcard();
        t.setTravelcardType(req.travelcardType());
        t.setTravelcardValidFrom(req.travelcardValidFrom());
        t.setTravelcardValidTo(req.travelcardValidTo());
        t.setTravelcardName(req.travelcardName());
        t.setTravelcardNumber(req.travelcardNumber());
        t.setTravelcardRequestedDate(req.travelcardRequestedDate());
        t.setTravelcardTransactionReference(req.travelcardTransactionReference());
        t.setTravelcardUsableTo(req.travelcardUsableTo());

        Travelcard saved = travelcardRepository.save(t);

        for (CardholderRequest cr : chList) {
            Cardholder ch = new Cardholder();
            ch.setTravelcardId(saved.getId());
            ch.setCardholderTitle(cr.cardholderTitle());
            ch.setCardholderForename(cr.cardholderForename());
            ch.setCardholderSurname(cr.cardholderSurname());
            ch.setCardholderType(cr.cardholderType());
            ch.setCardholderPhotoName(cr.cardholderPhotoName());
            ch.setCardholderPhotoRrsKey(cr.cardholderPhotoRRSKey());
            ch.setCardholderPhotoUrl(cr.cardholderPhotoURL());
            ch.setCardholderPhotoKey(cr.cardholderPhotoKey());
            cardholderRepository.save(ch);
        }

        String travelcardId = UUID.randomUUID().toString();
        String token = generateToken();

        log.info("Exit createTravelcard travelcardDbId={} travelcardId={}", saved.getId(), travelcardId);

        return new TravelcardResponse(travelcardId, token);
    }

    private boolean allowsSecondary(TravelcardType type) {
        // Business rule: only TwoTogether and Family allow a secondary cardholder
        return type == TravelcardType.TwoTogether || type == TravelcardType.Family;
    }

    private String generateToken() {
        String alpha = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        StringBuilder sb = new StringBuilder(6);
        for (int i = 0; i < 6; i++) {
            int idx = (int) (Math.random() * alpha.length());
            sb.append(alpha.charAt(idx));
        }
        return sb.toString();
    }
}
