package com.ai2dev.test_tc_sb_api.service;

import com.ai2dev.test_tc_sb_api.dto.CardholderRequest;
import com.ai2dev.test_tc_sb_api.dto.TravelcardRequest;
import com.ai2dev.test_tc_sb_api.dto.TravelcardResponse;
import com.ai2dev.test_tc_sb_api.model.Cardholder;
import com.ai2dev.test_tc_sb_api.model.CardholderType;
import com.ai2dev.test_tc_sb_api.model.Travelcard;
import com.ai2dev.test_tc_sb_api.model.TravelcardType;
import com.ai2dev.test_tc_sb_api.repository.CardholderRepository;
import com.ai2dev.test_tc_sb_api.repository.TravelcardRepository;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.OffsetDateTime;
import java.time.ZoneOffset;
import java.util.EnumSet;
import java.util.List;
import java.util.Locale;
import java.util.Objects;
import java.util.Random;
import java.util.Set;
import java.util.stream.Collectors;

@Service
public class TravelcardService {
    private static final Logger logger = LoggerFactory.getLogger(TravelcardService.class);

    private final TravelcardRepository travelcardRepository;
    private final CardholderRepository cardholderRepository;

    private static final Set<TravelcardType> ALLOW_SECONDARY = EnumSet.of(TravelcardType.TwoTogether, TravelcardType.Family, TravelcardType.Network);

    public TravelcardService(TravelcardRepository travelcardRepository, CardholderRepository cardholderRepository) {
        this.travelcardRepository = travelcardRepository;
        this.cardholderRepository = cardholderRepository;
    }

    @Transactional
    public TravelcardResponse create(TravelcardRequest request) {
        logger.info("Entering create with travelcardNumber={}", request.getTravelcardNumber());

        validateBusinessRules(request);

        Travelcard t = new Travelcard();
        t.setTravelcardType(request.getTravelcardType());
        t.setTravelcardValidFrom(request.getTravelcardValidFrom());
        t.setTravelcardValidTo(request.getTravelcardValidTo());
        t.setTravelcardName(request.getTravelcardName());
        t.setTravelcardNumber(request.getTravelcardNumber());
        t.setTravelcardRequestedDate(request.getTravelcardRequestedDate());
        t.setTravelcardTransactionReference(request.getTravelcardTransactionReference());
        t.setTravelcardUsableTo(request.getTravelcardUsableTo());

        Travelcard saved = travelcardRepository.save(t);

        List<Cardholder> holders = request.getCardholders().stream().map(this::toEntity).collect(Collectors.toList());
        for (Cardholder h : holders) {
            h.setTravelcardId(saved.getId());
            cardholderRepository.save(h);
        }

        String token = generateToken();

        logger.info("Exiting create with id={}", saved.getId());
        return new TravelcardResponse(String.valueOf(saved.getId()), token);
    }

    public Travelcard getById(Integer id) {
        logger.info("Fetching travelcard id={}", id);
        return travelcardRepository.findById(id).orElse(null);
    }

    public List<Cardholder> getCardholdersFor(Integer travelcardId) {
        logger.info("Fetching cardholders for travelcard id={}", travelcardId);
        return cardholderRepository.findByTravelcardId(travelcardId);
    }

    private Cardholder toEntity(CardholderRequest r) {
        Cardholder h = new Cardholder();
        h.setCardholderTitle(r.getCardholderTitle());
        h.setCardholderForename(r.getCardholderForename());
        h.setCardholderSurname(r.getCardholderSurname());
        h.setCardholderType(r.getCardholderType());
        h.setCardholderPhotoName(r.getCardholderPhotoName());
        h.setCardholderPhotoUrl(r.getCardholderPhotoURL());
        h.setCardholderPhotoKey(r.getCardholderPhotoKey());
        return h;
    }

    private void validateBusinessRules(TravelcardRequest r) {
        OffsetDateTime now = OffsetDateTime.now(ZoneOffset.UTC);

        if (!r.getTravelcardRequestedDate().isBefore(now)) {
            logger.error("Requested date must be in the past: {}", r.getTravelcardRequestedDate());
            throw new IllegalArgumentException("travelcardRequestedDate must be in the past");
        }

        if (r.getTravelcardValidFrom().isAfter(r.getTravelcardValidTo())) {
            logger.error("Valid from is after valid to: {} > {}", r.getTravelcardValidFrom(), r.getTravelcardValidTo());
            throw new IllegalArgumentException("travelcardValidFrom must not be after travelcardValidTo");
        }

        if (!r.getTravelcardValidTo().isAfter(now)) {
            logger.error("Valid to must be in the future: {}", r.getTravelcardValidTo());
            throw new IllegalArgumentException("travelcardValidTo must be in the future");
        }

        OffsetDateTime maxValidFrom = r.getTravelcardRequestedDate().plusMonths(1);
        if (r.getTravelcardValidFrom().isAfter(maxValidFrom)) {
            logger.error("Valid from {} is later than one calendar month from requested date {}", r.getTravelcardValidFrom(), r.getTravelcardRequestedDate());
            throw new IllegalArgumentException("travelcardValidFrom must be no later than one calendar month from travelcardRequestedDate");
        }

        if (r.getTravelcardType() == TravelcardType.SixteenToSeventeen) {
            if (r.getTravelcardUsableTo() == null) {
                logger.error("usableTo required for SixteenToSeventeen");
                throw new IllegalArgumentException("travelcardUsableTo is required for SixteenToSeventeen type");
            }
            if (!r.getTravelcardUsableTo().isAfter(now)) {
                logger.error("usableTo must be in the future: {}", r.getTravelcardUsableTo());
                throw new IllegalArgumentException("travelcardUsableTo must be in the future");
            }
        } else {
            if (r.getTravelcardUsableTo() != null) {
                logger.error("usableTo must be null for non-SixteenToSeventeen types");
                throw new IllegalArgumentException("travelcardUsableTo must be null unless TravelcardType is SixteenToSeventeen");
            }
        }

        List<CardholderRequest> ch = r.getCardholders();
        if (ch == null || ch.size() < 1 || ch.size() > 2) {
            logger.error("Cardholders must be 1 or 2, actual={}", ch == null ? 0 : ch.size());
            throw new IllegalArgumentException("cardholders must contain exactly 1 or 2 items");
        }

        long primaryCount = ch.stream().filter(c -> c.getCardholderType() == CardholderType.Primary).count();
        long secondaryCount = ch.stream().filter(c -> c.getCardholderType() == CardholderType.Secondary).count();

        if (primaryCount != 1) {
            logger.error("There must be exactly one Primary cardholder, foundPrimary={}", primaryCount);
            throw new IllegalArgumentException("Exactly one Primary cardholder is required");
        }

        if (secondaryCount > 1) {
            logger.error("At most one Secondary cardholder is allowed, foundSecondary={}", secondaryCount);
            throw new IllegalArgumentException("At most one Secondary cardholder is allowed");
        }

        if (secondaryCount == 1 && !ALLOW_SECONDARY.contains(r.getTravelcardType())) {
            logger.error("Secondary cardholder not allowed for type={}", r.getTravelcardType());
            throw new IllegalArgumentException("Secondary cardholder not allowed for this Travelcard type");
        }

        for (CardholderRequest c : ch) {
            boolean hasUrl = c.getCardholderPhotoURL() != null && !c.getCardholderPhotoURL().isBlank();
            boolean hasKey = c.getCardholderPhotoKey() != null && !c.getCardholderPhotoKey().isBlank();
            if (hasUrl == hasKey) {
                logger.error("Cardholder must have exactly one of photo URL or photo key, hasUrl={}, hasKey={}", hasUrl, hasKey);
                throw new IllegalArgumentException("Each cardholder must provide exactly one of cardholderPhotoURL or cardholderPhotoKey");
            }
        }
    }

    private String generateToken() {
        int length = 6;
        String chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        Random rnd = new Random();
        StringBuilder sb = new StringBuilder(length);
        for (int i = 0; i < length; i++) {
            sb.append(chars.charAt(rnd.nextInt(chars.length())));
        }
        return sb.toString();
    }
}
