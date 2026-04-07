package com.ai2dev.demo_travelcard_sb.service;

import com.ai2dev.demo_travelcard_sb.dto.CardholderDto;
import com.ai2dev.demo_travelcard_sb.dto.CreateTravelcardRequest;
import com.ai2dev.demo_travelcard_sb.dto.CreateTravelcardResponse;
import com.ai2dev.demo_travelcard_sb.model.Cardholder;
import com.ai2dev.demo_travelcard_sb.model.Travelcard;
import com.ai2dev.demo_travelcard_sb.model.TravelcardType;
import com.ai2dev.demo_travelcard_sb.repository.TravelcardRepository;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import java.time.OffsetDateTime;
import java.time.ZoneOffset;
import java.util.HashSet;
import java.util.List;
import java.util.Random;
import java.util.Set;
import java.util.stream.Collectors;

@Service
public class TravelcardService {
    private static final Logger log = LoggerFactory.getLogger(TravelcardService.class);

    private final TravelcardRepository travelcardRepository;

    public TravelcardService(TravelcardRepository travelcardRepository) {
        this.travelcardRepository = travelcardRepository;
    }

    @Transactional
    public CreateTravelcardResponse createTravelcard(CreateTravelcardRequest req) {
        log.info("Enter createTravelcard service");

        OffsetDateTime now = OffsetDateTime.now(ZoneOffset.UTC);

        if (req.travelcardRequestedDate().isAfter(now)) {
            throw new IllegalArgumentException("travelcardRequestedDate must be in the past");
        }

        if (req.travelcardValidFrom().isAfter(req.travelcardValidTo())) {
            throw new IllegalArgumentException("travelcardValidFrom must not be after travelcardValidTo");
        }

        if (req.travelcardValidTo().isBefore(now)) {
            throw new IllegalArgumentException("travelcardValidTo must be in the future");
        }

        if (req.travelcardType() == TravelcardType.SixteenToSeventeen) {
            if (req.travelcardUsableTo() == null) {
                throw new IllegalArgumentException("travelcardUsableTo is required for SixteenToSeventeen type");
            }

            if (req.travelcardUsableTo().isBefore(now)) {
                throw new IllegalArgumentException("travelcardUsableTo must be in the future");
            }
        }

        List<CardholderDto> ch = req.cardholders();
        if (ch == null || ch.size() < 1 || ch.size() > 2) {
            throw new IllegalArgumentException("cardholders must contain exactly one Primary and optional one Secondary");
        }

        long primaryCount = ch.stream().filter(c -> c.cardholderType() != null && c.cardholderType().name().equals("Primary")).count();
        if (primaryCount != 1) {
            throw new IllegalArgumentException("Exactly one Primary cardholder is required");
        }

        boolean hasSecondary = ch.stream().anyMatch(c -> c.cardholderType() != null && c.cardholderType().name().equals("Secondary"));
        if (hasSecondary) {
            Set<TravelcardType> allowed = new HashSet<>();
            allowed.add(TravelcardType.TwoTogether);
            allowed.add(TravelcardType.Family);

            if (!allowed.contains(req.travelcardType())) {
                throw new IllegalArgumentException("Secondary cardholder is not allowed for this Travelcard type");
            }
        }

        Travelcard toSave = new Travelcard();
        toSave.setTravelcardType(req.travelcardType());
        toSave.setTravelcardValidFrom(req.travelcardValidFrom());
        toSave.setTravelcardValidTo(req.travelcardValidTo());
        toSave.setTravelcardName(req.travelcardName());
        toSave.setTravelcardNumber(req.travelcardNumber());
        toSave.setTravelcardRequestedDate(req.travelcardRequestedDate());
        toSave.setTravelcardTransactionReference(req.travelcardTransactionReference());
        toSave.setTravelcardUsableTo(req.travelcardUsableTo());

        Set<Cardholder> cardholders = ch.stream().map(dto -> {
            Cardholder c = new Cardholder();
            c.setCardholderTitle(dto.cardholderTitle());
            c.setCardholderForename(dto.cardholderForename());
            c.setCardholderSurname(dto.cardholderSurname());
            c.setCardholderType(dto.cardholderType());
            c.setCardholderPhotoName(dto.cardholderPhotoName());
            c.setCardholderPhotoRrsKey(dto.cardholderPhotoRrsKey());
            c.setCardholderPhotoUrl(dto.cardholderPhotoUrl());
            c.setCardholderPhotoKey(dto.cardholderPhotoKey());
            return c;
        }).collect(Collectors.toCollection(HashSet::new));

        toSave.setCardholders(cardholders);

        Travelcard saved = travelcardRepository.save(toSave);

        String token = generateToken(6);

        CreateTravelcardResponse resp = new CreateTravelcardResponse(saved.getId(), token);
        log.info("Exit createTravelcard service");
        return resp;
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
