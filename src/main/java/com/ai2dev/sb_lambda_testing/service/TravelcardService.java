package com.ai2dev.sb_lambda_testing.service;

import com.ai2dev.sb_lambda_testing.dto.CardholderRequest;
import com.ai2dev.sb_lambda_testing.dto.TravelcardRequest;
import com.ai2dev.sb_lambda_testing.dto.TravelcardResponse;
import com.ai2dev.sb_lambda_testing.model.Cardholder;
import com.ai2dev.sb_lambda_testing.model.CardholderType;
import com.ai2dev.sb_lambda_testing.model.Travelcard;
import com.ai2dev.sb_lambda_testing.model.TravelcardType;
import com.ai2dev.sb_lambda_testing.repository.CardholderRepository;
import com.ai2dev.sb_lambda_testing.repository.TravelcardRepository;
import java.time.OffsetDateTime;
import java.time.ZoneOffset;
import java.util.List;
import java.util.UUID;
import java.util.concurrent.ThreadLocalRandom;
import java.util.stream.Collectors;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

@Service
public class TravelcardService {
    private static final Logger logger = LoggerFactory.getLogger(TravelcardService.class);

    private final TravelcardRepository travelcardRepository;

    private final CardholderRepository cardholderRepository;

    public TravelcardService(TravelcardRepository travelcardRepository,
        CardholderRepository cardholderRepository) {
        this.travelcardRepository = travelcardRepository;
        this.cardholderRepository = cardholderRepository;
    }

    @Transactional
    public TravelcardResponse createTravelcard(TravelcardRequest req, String clientId, String correlationId) {
        logger.info("Enter createTravelcard request.transactionReference={} clientId={}", req.travelcardTransactionReference(), clientId);

        validateBusinessRules(req);

        Travelcard travelcard = new Travelcard();

        travelcard.setTravelcardType(TravelcardType.valueOf(req.travelcardType().name()));
        travelcard.setTravelcardValidFrom(req.travelcardValidFrom());
        travelcard.setTravelcardValidTo(req.travelcardValidTo());
        travelcard.setTravelcardName(req.travelcardName());
        travelcard.setTravelcardNumber(req.travelcardNumber());
        travelcard.setTravelcardRequestedDate(req.travelcardRequestedDate());
        travelcard.setTravelcardTransactionReference(req.travelcardTransactionReference());
        travelcard.setTravelcardUsableTo(req.travelcardUsableTo());

        Travelcard saved = travelcardRepository.save(travelcard);

        List<Cardholder> cardholders = req.cardholders().stream()
            .map(this::toCardholder)
            .collect(Collectors.toList());

        for (Cardholder ch : cardholders) {
            ch.setTravelcardId(saved.getId());
            cardholderRepository.save(ch);
        }

        String travelcardId = UUID.randomUUID().toString();

        String token = generateToken();

        logger.info("Exit createTravelcard dbId={} travelcardId={}", saved.getId(), travelcardId);

        return new TravelcardResponse(travelcardId, token);
    }

    private Cardholder toCardholder(CardholderRequest r) {
        Cardholder c = new Cardholder();
        c.setCardholderTitle(r.cardholderTitle());
        c.setCardholderForename(r.cardholderForename());
        c.setCardholderSurname(r.cardholderSurname());
        c.setCardholderType(CardholderType.valueOf(r.cardholderType().name()));
        c.setCardholderPhotoName(r.cardholderPhotoName());
        c.setCardholderPhotoRrsKey(r.cardholderPhotoRRSKey());
        c.setCardholderPhotoUrl(r.cardholderPhotoURL());
        c.setCardholderPhotoKey(r.cardholderPhotoKey());

        return c;
    }

    private void validateBusinessRules(TravelcardRequest req) {
        OffsetDateTime now = OffsetDateTime.now(ZoneOffset.UTC);

        if (!req.travelcardRequestedDate().isBefore(now) && !req.travelcardRequestedDate().isEqual(now)) {
            throw new IllegalArgumentException("requested_date must be in the past");
        }

        if (!req.travelcardValidFrom().isBefore(req.travelcardValidTo())) {
            throw new IllegalArgumentException("valid_from must be before valid_to");
        }

        if (!req.travelcardValidTo().isAfter(now) && !req.travelcardValidTo().isEqual(now)) {
            throw new IllegalArgumentException("valid_to must be in the future");
        }

        if (req.travelcardType() == TravelcardType.SixteenToSeventeen) {
            if (req.travelcardUsableTo() == null) {
                throw new IllegalArgumentException("usable_to is required for SixteenToSeventeen travelcards");
            }

            if (!req.travelcardUsableTo().isAfter(now)) {
                throw new IllegalArgumentException("usable_to must be in the future");
            }
        }

        if (req.cardholders() == null || req.cardholders().size() < 1 || req.cardholders().size() > 2) {
            throw new IllegalArgumentException("cardholders must contain exactly 1 or 2 items");
        }

        long primaryCount = req.cardholders().stream()
            .filter(c -> c.cardholderType() == com.ai2dev.sb_lambda_testing.model.CardholderType.Primary)
            .count();

        if (primaryCount != 1) {
            throw new IllegalArgumentException("There must be exactly one Primary cardholder");
        }

        if (req.cardholders().size() == 2) {
            boolean allowed = allowsSecondary(req.travelcardType());

            if (!allowed) {
                throw new IllegalArgumentException("Secondary cardholder is not allowed for travelcard type " + req.travelcardType());
            }
        }

        for (CardholderRequest ch : req.cardholders()) {
            if ((ch.cardholderPhotoRRSKey() == null || ch.cardholderPhotoRRSKey().isBlank())
                && (ch.cardholderPhotoURL() == null || ch.cardholderPhotoURL().isBlank())
                && (ch.cardholderPhotoKey() == null || ch.cardholderPhotoKey().isBlank())) {
                throw new IllegalArgumentException("Each cardholder must provide one of cardholder_photo_rrs_key, cardholder_photo_url or cardholder_photo_key");
            }
        }
    }

    private boolean allowsSecondary(TravelcardType type) {
        switch (type) {
            case TwoTogether:
            case Family:
                return true;
            default:
                return false;
        }
    }

    private String generateToken() {
        final String chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        StringBuilder sb = new StringBuilder(6);

        ThreadLocalRandom rnd = ThreadLocalRandom.current();

        for (int i = 0; i < 6; i++) {
            sb.append(chars.charAt(rnd.nextInt(chars.length())));
        }

        return sb.toString();
    }
}
