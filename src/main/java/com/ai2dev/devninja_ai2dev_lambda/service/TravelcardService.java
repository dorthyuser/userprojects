package com.ai2dev.devninja_ai2dev_lambda.service;

import com.ai2dev.devninja_ai2dev_lambda.dto.CardholderRequest;
import com.ai2dev.devninja_ai2dev_lambda.dto.TravelcardRequest;
import com.ai2dev.devninja_ai2dev_lambda.dto.TravelcardResponse;
import com.ai2dev.devninja_ai2dev_lambda.exception.ApiException;
import com.ai2dev.devninja_ai2dev_lambda.model.Cardholder;
import com.ai2dev.devninja_ai2dev_lambda.model.Travelcard;
import com.ai2dev.devninja_ai2dev_lambda.repository.CardholderRepository;
import com.ai2dev.devninja_ai2dev_lambda.repository.TravelcardRepository;
import jakarta.inject.Inject;
import jakarta.inject.Singleton;
import java.time.OffsetDateTime;
import java.util.List;
import java.util.Set;
import java.util.UUID;
import java.util.stream.Collectors;

@Singleton
public class TravelcardService {
    private static final org.slf4j.Logger LOG = org.slf4j.LoggerFactory.getLogger(TravelcardService.class);

    private static final Set<String> TYPES_ALLOWING_SECONDARY = Set.of("Family", "TwoTogether");

    @Inject
    TravelcardRepository travelcardRepository;

    @Inject
    CardholderRepository cardholderRepository;

    public TravelcardResponse createTravelcard(TravelcardRequest req) {
        LOG.info("{\"event\":\"entry\"}");
        try {
            // Basic validations
            OffsetDateTime now = OffsetDateTime.now();

            if (req.getTravelcardRequestedDate() == null || !req.getTravelcardRequestedDate().isBefore(now)) {
                throw new ApiException(400, "travelcardRequestedDate must be in the past");
            }

            if (req.getTravelcardValidFrom() == null || req.getTravelcardValidTo() == null) {
                throw new ApiException(400, "validFrom and validTo are required");
            }

            if (req.getTravelcardValidFrom().isAfter(req.getTravelcardValidTo())) {
                // Spec requires checking if valid_from date is later than valid_to date (i.e. invalid)
                throw new ApiException(400, "travelcardValidFrom must not be after travelcardValidTo");
            }

            if (!req.getTravelcardValidTo().isAfter(now)) {
                throw new ApiException(400, "travelcardValidTo must be in the future");
            }

            if ("SixteenToSeventeen".equals(req.getTravelcardType())) {
                if (req.getTravelcardUsableTo() == null) {
                    throw new ApiException(400, "travelcardUsableTo is required for SixteenToSeventeen type");
                }
                if (!req.getTravelcardUsableTo().isAfter(now)) {
                    throw new ApiException(400, "travelcardUsableTo must be in the future");
                }
            }

            List<CardholderRequest> cardholders = req.getCardholders();
            if (cardholders == null || cardholders.size() < 1 || cardholders.size() > 2) {
                throw new ApiException(400, "cardholders must contain 1 or 2 items");
            }

            long primaryCount = cardholders.stream().filter(c -> "Primary".equals(c.getCardholderType())).count();
            if (primaryCount != 1) {
                throw new ApiException(400, "Exactly one Primary cardholder is required");
            }

            boolean hasSecondary = cardholders.stream().anyMatch(c -> "Secondary".equals(c.getCardholderType()));
            if (hasSecondary && !TYPES_ALLOWING_SECONDARY.contains(req.getTravelcardType())) {
                throw new ApiException(400, "Secondary cardholder not allowed for this travelcard type");
            }

            // Ensure each cardholder has one photo field
            for (CardholderRequest ch : cardholders) {
                if ((ch.getCardholderPhotoRRSKey() == null || ch.getCardholderPhotoRRSKey().isBlank())
                    && (ch.getCardholderPhotoURL() == null || ch.getCardholderPhotoURL().isBlank())
                    && (ch.getCardholderPhotoKey() == null || ch.getCardholderPhotoKey().isBlank())) {
                    throw new ApiException(400, "Each cardholder must provide one photo reference");
                }
            }

            // Persist travelcard
            Travelcard travelcard = new Travelcard();
            travelcard.setTravelcardType(req.getTravelcardType());
            travelcard.setTravelcardValidFrom(req.getTravelcardValidFrom());
            travelcard.setTravelcardValidTo(req.getTravelcardValidTo());
            travelcard.setTravelcardName(req.getTravelcardName());
            travelcard.setTravelcardNumber(req.getTravelcardNumber());
            travelcard.setTravelcardRequestedDate(req.getTravelcardRequestedDate());
            travelcard.setTravelcardTransactionReference(req.getTravelcardTransactionReference());
            travelcard.setTravelcardUsableTo(req.getTravelcardUsableTo());

            Travelcard saved = travelcardRepository.save(travelcard);

            // Persist cardholders
            List<Cardholder> savedCardholders = cardholders.stream().map(ch -> {
                Cardholder card = new Cardholder();
                card.setTravelcardId(saved.getId());
                card.setCardholderTitle(ch.getCardholderTitle());
                card.setCardholderForename(ch.getCardholderForename());
                card.setCardholderSurname(ch.getCardholderSurname());
                card.setCardholderType(ch.getCardholderType());
                card.setCardholderPhotoName(ch.getCardholderPhotoName());
                card.setCardholderPhotoRRSKey(ch.getCardholderPhotoRRSKey());
                card.setCardholderPhotoURL(ch.getCardholderPhotoURL());
                card.setCardholderPhotoKey(ch.getCardholderPhotoKey());
                return cardholderRepository.save(card);
            }).collect(Collectors.toList());

            // Build response
            String travelcardId = UUID.randomUUID().toString();
            String token = generateToken();

            TravelcardResponse response = new TravelcardResponse();
            response.setTravelcardId(travelcardId);
            response.setToken(token);

            LOG.info("{\"event\":\"exit\",\"id\":\"{}\"}", travelcardId);
            return response;
        } catch (ApiException e) {
            LOG.error("{\"event\":\"error\",\"message\":\"{}\"}", e.getMessage(), e);
            throw e;
        } catch (Exception e) {
            LOG.error("{\"event\":\"error\",\"message\":\"{}\"}", e.getMessage(), e);
            throw new ApiException(500, "internal error");
        }
    }

    private String generateToken() {
        // Simple 6-char token
        String t = UUID.randomUUID().toString().replaceAll("[^A-Za-z0-9]", "");
        return t.substring(0, Math.min(6, t.length())).toUpperCase();
    }
}
