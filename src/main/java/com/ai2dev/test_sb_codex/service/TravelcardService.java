package com.ai2dev.test_sb_codex.service;

import com.ai2dev.test_sb_codex.dto.CardholderDto;
import com.ai2dev.test_sb_codex.dto.CreateTravelcardResponse;
import com.ai2dev.test_sb_codex.dto.TravelcardRequest;
import com.ai2dev.test_sb_codex.model.Cardholder;
import com.ai2dev.test_sb_codex.model.CardholderType;
import com.ai2dev.test_sb_codex.model.Travelcard;
import com.ai2dev.test_sb_codex.model.TravelcardType;
import com.ai2dev.test_sb_codex.repository.TravelcardRepository;
import java.time.Instant;
import java.util.HashSet;
import java.util.List;
import java.util.Set;
import java.util.UUID;
import java.util.concurrent.ThreadLocalRandom;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;

@Service
public class TravelcardService {
    private static final Logger log = LoggerFactory.getLogger(TravelcardService.class);
    private final TravelcardRepository travelcardRepository;

    public TravelcardService(TravelcardRepository travelcardRepository) {
        this.travelcardRepository = travelcardRepository;
    }

    public CreateTravelcardResponse createTravelcard(TravelcardRequest request) {
        log.info("Entered TravelcardService.createTravelcard");

        validateBusinessRules(request);

        Travelcard entity = mapToEntity(request);

        Travelcard saved = travelcardRepository.save(entity);

        String travelcardId = UUID.randomUUID().toString();
        String token = generateToken();

        log.info("Travelcard created with db id: {}", saved.getId());
        log.info("Exiting TravelcardService.createTravelcard");
        return new CreateTravelcardResponse(travelcardId, token);
    }

    private Travelcard mapToEntity(TravelcardRequest req) {
        Travelcard t = new Travelcard();
        t.setTravelcardType(TravelcardType.valueOf(req.travelcardType()));
        t.setTravelcardValidFrom(req.travelcardValidFrom());
        t.setTravelcardValidTo(req.travelcardValidTo());
        t.setTravelcardName(req.travelcardName());
        t.setTravelcardNumber(req.travelcardNumber());
        t.setTravelcardRequestedDate(req.travelcardRequestedDate());
        t.setTravelcardTransactionReference(req.travelcardTransactionReference());
        t.setTravelcardUsableTo(req.travelcardUsableTo());

        List<CardholderDto> ch = req.cardholders();
        Set<Cardholder> set = new HashSet<>();
        for (CardholderDto d : ch) {
            Cardholder c = new Cardholder();
            c.setCardholderTitle(d.cardholderTitle());
            c.setCardholderForename(d.cardholderForename());
            c.setCardholderSurname(d.cardholderSurname());
            c.setCardholderType(CardholderType.valueOf(d.cardholderType()));
            c.setCardholderPhotoName(d.cardholderPhotoName());
            c.setCardholderPhotoRRSKey(d.cardholderPhotoRRSKey());
            c.setCardholderPhotoURL(d.cardholderPhotoURL());
            c.setCardholderPhotoKey(d.cardholderPhotoKey());
            set.add(c);
        }
        t.setCardholders(set);
        return t;
    }

    private void validateBusinessRules(TravelcardRequest req) {
        Instant now = Instant.now();

        // requested_date is in the past or present
        if (req.travelcardRequestedDate().isAfter(now)) {
            throw new IllegalArgumentException("requested date must be in the past or present");
        }

        // valid_from must be before valid_to
        if (!req.travelcardValidFrom().isBefore(req.travelcardValidTo())) {
            throw new IllegalArgumentException("valid_from must be before valid_to");
        }

        // valid_to must be in the future
        if (!req.travelcardValidTo().isAfter(now)) {
            throw new IllegalArgumentException("valid_to must be in the future");
        }

        // If SixteenToSeventeen, usable_to is required and must be in the future
        if ("SixteenToSeventeen".equals(req.travelcardType())) {
            if (req.travelcardUsableTo() == null) {
                throw new IllegalArgumentException("usable_to is required for SixteenToSeventeen");
            }
            if (!req.travelcardUsableTo().isAfter(now)) {
                throw new IllegalArgumentException("usable_to must be in the future");
            }
        }

        // cardholders count 1 or 2 and exactly one Primary
        int size = req.cardholders().size();
        if (size < 1 || size > 2) {
            throw new IllegalArgumentException("cardholders must be 1 or 2 items");
        }
        long primaryCount = req.cardholders().stream().filter(c -> "Primary".equals(c.cardholderType())).count();
        if (primaryCount != 1) {
            throw new IllegalArgumentException("exactly one Primary cardholder required");
        }

        // secondary allowed only for certain types (TwoTogether, Family)
        boolean hasSecondary = req.cardholders().stream().anyMatch(c -> "Secondary".equals(c.cardholderType()));
        if (hasSecondary) {
            String t = req.travelcardType();
            if (!("TwoTogether".equals(t) || "Family".equals(t))) {
                throw new IllegalArgumentException("Secondary cardholder is not allowed for this travelcard type");
            }
        }

        // Each cardholder must have exactly one photo identifier
        for (CardholderDto c : req.cardholders()) {
            int provided = 0;
            if (c.cardholderPhotoRRSKey() != null && !c.cardholderPhotoRRSKey().isBlank()) {
                provided++;
            }
            if (c.cardholderPhotoURL() != null && !c.cardholderPhotoURL().isBlank()) {
                provided++;
            }
            if (c.cardholderPhotoKey() != null && !c.cardholderPhotoKey().isBlank()) {
                provided++;
            }
            if (provided != 1) {
                throw new IllegalArgumentException("each cardholder must provide exactly one photo identifier");
            }
        }
    }

    private String generateToken() {
        String alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        StringBuilder sb = new StringBuilder(6);
        ThreadLocalRandom rnd = ThreadLocalRandom.current();
        for (int i = 0; i < 6; i++) {
            sb.append(alphabet.charAt(rnd.nextInt(alphabet.length())));
        }
        return sb.toString();
    }
}
