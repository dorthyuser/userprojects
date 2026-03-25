package com.ai2dev.test_sb_java_travelcard.service;

import com.ai2dev.test_sb_java_travelcard.dto.CardholderRequest;
import com.ai2dev.test_sb_java_travelcard.dto.TravelcardRequest;
import com.ai2dev.test_sb_java_travelcard.dto.TravelcardResponse;
import com.ai2dev.test_sb_java_travelcard.model.Cardholder;
import com.ai2dev.test_sb_java_travelcard.model.Travelcard;
import com.ai2dev.test_sb_java_travelcard.model.enums.CardholderType;
import com.ai2dev.test_sb_java_travelcard.model.enums.TravelcardType;
import com.ai2dev.test_sb_java_travelcard.repository.CardholderRepository;
import com.ai2dev.test_sb_java_travelcard.repository.TravelcardRepository;
import java.time.OffsetDateTime;
import java.util.List;
import java.util.Set;
import java.util.UUID;
import java.util.concurrent.ThreadLocalRandom;
import java.util.stream.Collectors;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.web.server.ResponseStatusException;
import org.springframework.http.HttpStatus;

@Service
public class TravelcardService {
    private static final Logger log = LoggerFactory.getLogger(TravelcardService.class);

    private final TravelcardRepository travelcardRepository;
    private final CardholderRepository cardholderRepository;

    private static final Set<TravelcardType> ALLOW_SECONDARY = Set.of(TravelcardType.TwoTogether, TravelcardType.Family);

    public TravelcardService(TravelcardRepository travelcardRepository, CardholderRepository cardholderRepository) {
        this.travelcardRepository = travelcardRepository;
        this.cardholderRepository = cardholderRepository;
    }

    @Transactional
    public TravelcardResponse createTravelcard(TravelcardRequest req) {
        log.info("Entered TravelcardService.createTravelcard");

        OffsetDateTime now = OffsetDateTime.now();

        // Business validations
        if (!req.travelcardRequestedDate().isBefore(now)) {
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "travelcardRequestedDate must be in the past");
        }

        if (!req.travelcardValidFrom().isBefore(req.travelcardValidTo())) {
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "travelcardValidFrom must be before travelcardValidTo");
        }

        if (!req.travelcardValidTo().isAfter(now)) {
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "travelcardValidTo must be in the future");
        }

        if (req.travelcardType() == TravelcardType.SixteenToSeventeen) {
            if (req.travelcardUsableTo() == null) {
                throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "travelcardUsableTo is required for SixteenToSeventeen");
            }

            if (!req.travelcardUsableTo().isAfter(now)) {
                throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "travelcardUsableTo must be in the future");
            }
        }

        // Cardholders validations
        List<CardholderRequest> ch = req.cardholders();

        if (ch == null || ch.isEmpty() || ch.size() > 2) {
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "Must provide 1 or 2 cardholders");
        }

        long primaryCount = ch.stream().filter(c -> c.cardholderType() == CardholderType.Primary).count();

        if (primaryCount != 1) {
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "Exactly one Primary cardholder required");
        }

        boolean hasSecondary = ch.stream().anyMatch(c -> c.cardholderType() == CardholderType.Secondary);

        if (hasSecondary && !ALLOW_SECONDARY.contains(req.travelcardType())) {
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "Secondary cardholder not allowed for this travelcard type");
        }

        // travelcardName pattern
        if (req.travelcardName() != null && !req.travelcardName().matches("^[A-Za-z0-9 ]*$")) {
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "travelcardName invalid pattern");
        }

        if (req.travelcardNumber() == null || req.travelcardNumber().length() < 11 || req.travelcardNumber().length() > 22 || !req.travelcardNumber().matches("^[A-Za-z0-9]+$")) {
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "travelcardNumber invalid");
        }

        if (req.travelcardTransactionReference() == null || req.travelcardTransactionReference().length() != 15 || !req.travelcardTransactionReference().matches("^[0-9]{2}[A-Z0-9]{4}[0-9]{4}[0-9]{5}$")) {
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "travelcardTransactionReference invalid");
        }

        // validate each cardholder image oneOf
        for (CardholderRequest c : ch) {
            int provided = 0;
            if (c.cardholderPhotoRRSKey() != null) provided++;
            if (c.cardholderPhotoURL() != null) provided++;
            if (c.cardholderPhotoKey() != null) provided++;

            if (provided != 1) {
                throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "Each cardholder must provide exactly one photo reference");
            }

            if (c.cardholderPhotoRRSKey() != null) {
                if (c.cardholderPhotoRRSKey().length() < 39 || c.cardholderPhotoRRSKey().length() > 42) {
                    throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "cardholderPhotoRRSKey length invalid");
                }
            }

            if (c.cardholderPhotoURL() != null) {
                if (c.cardholderPhotoURL().length() < 20 || c.cardholderPhotoURL().length() > 2048) {
                    throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "cardholderPhotoURL length invalid");
                }
            }

            if (c.cardholderPhotoKey() != null) {
                if (c.cardholderPhotoKey().length() < 39 || c.cardholderPhotoKey().length() > 42) {
                    throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "cardholderPhotoKey length invalid");
                }
            }
        }

        // Persist travelcard
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

        List<Cardholder> cardholders = ch.stream().map(c -> {
            Cardholder chEnt = new Cardholder();
            chEnt.setTravelcardId(saved.getId());
            chEnt.setCardholderTitle(c.cardholderTitle());
            chEnt.setCardholderForename(c.cardholderForename());
            chEnt.setCardholderSurname(c.cardholderSurname());
            chEnt.setCardholderType(c.cardholderType());
            chEnt.setCardholderPhotoName(c.cardholderPhotoName());
            chEnt.setCardholderPhotoRRSKey(c.cardholderPhotoRRSKey());
            chEnt.setCardholderPhotoURL(c.cardholderPhotoURL());
            chEnt.setCardholderPhotoKey(c.cardholderPhotoKey());
            return chEnt;
        }).collect(Collectors.toList());

        cardholderRepository.saveAll(cardholders);

        String uuid = UUID.randomUUID().toString();
        String token = generateToken();

        log.info("Exiting TravelcardService.createTravelcard");
        return new TravelcardResponse(uuid, token);
    }

    private String generateToken() {
        StringBuilder sb = new StringBuilder();
        String chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        ThreadLocalRandom rnd = ThreadLocalRandom.current();
        for (int i = 0; i < 6; i++) {
            sb.append(chars.charAt(rnd.nextInt(chars.length())));
        }
        return sb.toString();
    }
}
