package com.ai2dev.demo_travelcards_spring.service;

import com.ai2dev.demo_travelcards_spring.dto.CardholderRequest;
import com.ai2dev.demo_travelcards_spring.dto.CreateTravelcardResponse;
import com.ai2dev.demo_travelcards_spring.dto.TravelcardRequest;
import com.ai2dev.demo_travelcards_spring.model.Cardholder;
import com.ai2dev.demo_travelcards_spring.model.CardholderType;
import com.ai2dev.demo_travelcards_spring.model.Travelcard;
import com.ai2dev.demo_travelcards_spring.repository.CardholderRepository;
import com.ai2dev.demo_travelcards_spring.repository.TravelcardRepository;
import java.security.SecureRandom;
import java.time.OffsetDateTime;
import java.util.ArrayList;
import java.util.List;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.cache.annotation.Cacheable;
import org.springframework.stereotype.Service;

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

    public CreateTravelcardResponse createTravelcard(TravelcardRequest req) {
        log.info("createTravelcard service - entry");

        Travelcard tc = new Travelcard();
        tc.setTravelcardType(req.travelcardType());
        tc.setTravelcardValidFrom(req.travelcardValidFrom());
        tc.setTravelcardValidTo(req.travelcardValidTo());
        tc.setTravelcardName(req.travelcardName());
        tc.setTravelcardNumber(req.travelcardNumber());
        tc.setTravelcardRequestedDate(req.travelcardRequestedDate());
        tc.setTravelcardTransactionReference(req.travelcardTransactionReference());
        tc.setTravelcardUsableTo(req.travelcardUsableTo());

        List<Cardholder> chs = new ArrayList<>();
        for (CardholderRequest r : req.cardholders()) {
            Cardholder c = new Cardholder();
            c.setCardholderTitle(r.cardholderTitle());
            c.setCardholderForename(r.cardholderForename());
            c.setCardholderSurname(r.cardholderSurname());
            c.setCardholderType(CardholderType.valueOf(r.cardholderType().name()));
            c.setCardholderPhotoName(r.cardholderPhotoName());
            c.setCardholderPhotoURL(r.cardholderPhotoURL());
            c.setCardholderPhotoKey(r.cardholderPhotoKey());
            chs.add(c);
        }

        tc.setCardholders(chs);

        Travelcard saved = travelcardRepository.save(tc);

        // associate and save cardholders if necessary (Spring Data JDBC may persist child collection automatically)
        if (saved.getCardholders() != null) {
            for (Cardholder c : saved.getCardholders()) {
                if (c.getTravelcardId() == null) {
                    c.setTravelcardId(saved.getId());
                }
                cardholderRepository.save(c);
            }
        }

        String token = generateToken(6);

        log.info("createTravelcard service - exit id={}", saved.getId());
        return new CreateTravelcardResponse(saved.getId(), token);
    }

    @Cacheable("travelcardById")
    public Travelcard getTravelcard(Long id) {
        log.info("getTravelcard - entry id={}", id);
        var res = travelcardRepository.findById(id).orElse(null);
        log.info("getTravelcard - exit id={}", id);
        return res;
    }

    private String generateToken(int length) {
        String chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        StringBuilder sb = new StringBuilder(length);
        for (int i = 0; i < length; i++) {
            sb.append(chars.charAt(random.nextInt(chars.length())));
        }
        return sb.toString();
    }
}
