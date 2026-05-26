package com.ai2dev.javasbtesting237.service;

import com.ai2dev.javasbtesting237.model.CardholderEntity;
import com.ai2dev.javasbtesting237.model.TravelcardEntity;
import com.ai2dev.javasbtesting237.model.dto.CardholderRequestDto;
import com.ai2dev.javasbtesting237.model.dto.CardholderType;
import com.ai2dev.javasbtesting237.model.dto.CreateTravelcardRequestDto;
import com.ai2dev.javasbtesting237.model.dto.CreateTravelcardResponseDto;
import com.ai2dev.javasbtesting237.model.dto.TravelcardType;
import com.ai2dev.javasbtesting237.repository.CardholderRepository;
import com.ai2dev.javasbtesting237.repository.TravelcardRepository;
import java.time.OffsetDateTime;
import java.util.ArrayList;
import java.util.List;
import java.util.UUID;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

@Service
public class TravelcardService {

    private final TravelcardRepository travelcardRepository;
    private final CardholderRepository cardholderRepository;

    public TravelcardService(TravelcardRepository travelcardRepository, CardholderRepository cardholderRepository) {
        this.travelcardRepository = travelcardRepository;
        this.cardholderRepository = cardholderRepository;
    }

    @Transactional
    public CreateTravelcardResponseDto createTravelcard(CreateTravelcardRequestDto request) {
        validateBusinessRules(request);

        TravelcardEntity travelcard = new TravelcardEntity(
                null,
                request.travelcardType(),
                request.travelcardValidFrom(),
                request.travelcardValidTo(),
                request.travelcardName(),
                request.travelcardNumber(),
                request.travelcardRequestedDate(),
                request.travelcardTransactionReference(),
                request.travelcardUsableTo());

        TravelcardEntity savedTravelcard = travelcardRepository.save(travelcard);

        List<CardholderEntity> entities = new ArrayList<>();
        for (CardholderRequestDto cardholder : request.cardholders()) {
            entities.add(new CardholderEntity(
                    null,
                    savedTravelcard.id(),
                    cardholder.cardholderTitle(),
                    cardholder.cardholderForename(),
                    cardholder.cardholderSurname(),
                    cardholder.cardholderType(),
                    cardholder.cardholderPhotoName(),
                    cardholder.cardholderPhotoRRSKey(),
                    cardholder.cardholderPhotoURL(),
                    cardholder.cardholderPhotoKey()));
        }
        cardholderRepository.saveAll(entities);

        String token = UUID.randomUUID().toString().replace("-", "").substring(0, 6).toUpperCase();
        return new CreateTravelcardResponseDto(String.valueOf(savedTravelcard.id()), token);
    }

    private void validateBusinessRules(CreateTravelcardRequestDto request) {
        OffsetDateTime now = OffsetDateTime.now();

        if (request.travelcardRequestedDate() == null || request.travelcardRequestedDate().isAfter(now)) {
            throw new IllegalArgumentException("travelcardRequestedDate must be in the past");
        }
        if (request.travelcardValidFrom() == null || request.travelcardValidTo() == null) {
            throw new IllegalArgumentException("travelcardValidFrom and travelcardValidTo are required");
        }
        if (request.travelcardValidFrom().isAfter(request.travelcardValidTo())) {
            throw new IllegalArgumentException("travelcardValidFrom must not be later than travelcardValidTo");
        }
        if (!request.travelcardValidTo().isAfter(now)) {
            throw new IllegalArgumentException("travelcardValidTo must be in the future");
        }
        if (request.travelcardValidFrom().isAfter(now.plusMonths(1))) {
            throw new IllegalArgumentException("travelcardValidFrom must not be later than one calendar month from today");
        }
        if (request.travelcardType() == TravelcardType.SixteenToSeventeen) {
            if (request.travelcardUsableTo() == null) {
                throw new IllegalArgumentException("travelcardUsableTo is required for SixteenToSeventeen");
            }
            if (!request.travelcardUsableTo().isAfter(now)) {
                throw new IllegalArgumentException("travelcardUsableTo must be in the future");
            }
        } else if (request.travelcardUsableTo() != null) {
            throw new IllegalArgumentException("travelcardUsableTo is only allowed for SixteenToSeventeen");
        }

        long primaryCount = request.cardholders().stream()
                .filter(c -> c.cardholderType() == CardholderType.Primary)
                .count();
        long secondaryCount = request.cardholders().stream()
                .filter(c -> c.cardholderType() == CardholderType.Secondary)
                .count();
        if (request.cardholders().size() < 1 || request.cardholders().size() > 2 || primaryCount != 1) {
            throw new IllegalArgumentException("Exactly one Primary cardholder and at most one Secondary cardholder are required");
        }
        if (secondaryCount > 1) {
            throw new IllegalArgumentException("Only one Secondary cardholder is allowed");
        }
        if (secondaryCount == 1 && (request.travelcardType() == TravelcardType.SixteenToSeventeen || request.travelcardType() == TravelcardType.Veterans)) {
            throw new IllegalArgumentException("Secondary cardholder is not allowed for this travelcard type");
        }
        for (CardholderRequestDto cardholder : request.cardholders()) {
            int provided = 0;
            if (cardholder.cardholderPhotoRRSKey() != null && !cardholder.cardholderPhotoRRSKey().isBlank()) provided++;
            if (cardholder.cardholderPhotoURL() != null && !cardholder.cardholderPhotoURL().isBlank()) provided++;
            if (cardholder.cardholderPhotoKey() != null && !cardholder.cardholderPhotoKey().isBlank()) provided++;
            if (provided != 1) {
                throw new IllegalArgumentException("Each cardholder must provide exactly one photo detail");
            }
        }
    }
}
