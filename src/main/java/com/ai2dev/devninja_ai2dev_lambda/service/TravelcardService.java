package com.ai2dev.devninja_ai2dev_lambda.service;

import com.ai2dev.devninja_ai2dev_lambda.dto.CardholderDto;
import com.ai2dev.devninja_ai2dev_lambda.dto.CreateTravelcardRequest;
import com.ai2dev.devninja_ai2dev_lambda.dto.CreateTravelcardResponse;
import com.ai2dev.devninja_ai2dev_lambda.model.Cardholder;
import com.ai2dev.devninja_ai2dev_lambda.model.CardholderType;
import com.ai2dev.devninja_ai2dev_lambda.model.Travelcard;
import com.ai2dev.devninja_ai2dev_lambda.model.TravelcardType;
import com.ai2dev.devninja_ai2dev_lambda.repository.CardholderRepository;
import com.ai2dev.devninja_ai2dev_lambda.repository.TravelcardRepository;
import jakarta.inject.Inject;
import jakarta.inject.Singleton;
import java.time.OffsetDateTime;
import java.util.List;
import java.util.UUID;
import java.util.concurrent.ThreadLocalRandom;

@Singleton
public class TravelcardService
{
    @Inject
    TravelcardRepository travelcardRepository;

    @Inject
    CardholderRepository cardholderRepository;

    public CreateTravelcardResponse createTravelcard(CreateTravelcardRequest request)
    {
        System.out.println("TravelcardService: enter createTravelcard");

        // Business validations
        OffsetDateTime now = OffsetDateTime.now();

        if (!request.getTravelcardRequestedDate().isBefore(now))
        {
            throw new IllegalArgumentException("travelcardRequestedDate must be in the past");
        }

        if (!request.getTravelcardValidFrom().isBefore(request.getTravelcardValidTo()))
        {
            throw new IllegalArgumentException("travelcardValidFrom must be before travelcardValidTo");
        }

        if (!request.getTravelcardValidTo().isAfter(now))
        {
            throw new IllegalArgumentException("travelcardValidTo must be in the future");
        }

        if (request.getTravelcardType() == TravelcardType.SixteenToSeventeen)
        {
            if (request.getTravelcardUsableTo() == null)
            {
                throw new IllegalArgumentException("travelcardUsableTo is required for SixteenToSeventeen");
            }

            if (!request.getTravelcardUsableTo().isAfter(now))
            {
                throw new IllegalArgumentException("travelcardUsableTo must be in the future");
            }
        }

        // Cardholder rules: 1 or 2 cardholders; exactly one Primary required; if type disallows secondary, ensure not provided
        List<CardholderDto> cardholders = request.getCardholders();

        if (cardholders == null || cardholders.size() < 1 || cardholders.size() > 2)
        {
            throw new IllegalArgumentException("cardholders must contain 1 or 2 items");
        }

        long primaryCount = cardholders.stream().filter(c -> c.getCardholderType() == CardholderType.Primary).count();

        if (primaryCount != 1)
        {
            throw new IllegalArgumentException("exactly one Primary cardholder is required");
        }

        // For demonstration assume SixteenToSeventeen does not allow Secondary
        if (request.getTravelcardType() == TravelcardType.SixteenToSeventeen)
        {
            boolean hasSecondary = cardholders.stream().anyMatch(c -> c.getCardholderType() == CardholderType.Secondary);
            if (hasSecondary)
            {
                throw new IllegalArgumentException("Secondary cardholder not allowed for SixteenToSeventeen travelcard type");
            }
        }

        // Validate each cardholder image one-of
        for (CardholderDto ch : cardholders)
        {
            int provided = 0;
            if (ch.getCardholderPhotoRRSKey() != null && !ch.getCardholderPhotoRRSKey().isBlank())
            {
                provided++;
            }
            if (ch.getCardholderPhotoURL() != null && !ch.getCardholderPhotoURL().isBlank())
            {
                provided++;
            }
            if (ch.getCardholderPhotoKey() != null && !ch.getCardholderPhotoKey().isBlank())
            {
                provided++;
            }

            if (provided != 1)
            {
                throw new IllegalArgumentException("Each cardholder must provide exactly one image reference (photoRRSKey, photoURL or photoKey)");
            }
        }

        // Persist travelcard
        Travelcard entity = new Travelcard();
        entity.setTravelcardType(request.getTravelcardType());
        entity.setTravelcardValidFrom(request.getTravelcardValidFrom());
        entity.setTravelcardValidTo(request.getTravelcardValidTo());
        entity.setTravelcardName(request.getTravelcardName());
        entity.setTravelcardNumber(request.getTravelcardNumber());
        entity.setTravelcardRequestedDate(request.getTravelcardRequestedDate());
        entity.setTravelcardTransactionReference(request.getTravelcardTransactionReference());
        entity.setTravelcardUsableTo(request.getTravelcardUsableTo());

        Travelcard saved = travelcardRepository.save(entity);

        // Persist cardholders
        for (CardholderDto ch : cardholders)
        {
            Cardholder cardholder = new Cardholder();
            cardholder.setTravelcardId(saved.getId());
            cardholder.setCardholderTitle(ch.getCardholderTitle());
            cardholder.setCardholderForename(ch.getCardholderForename());
            cardholder.setCardholderSurname(ch.getCardholderSurname());
            cardholder.setCardholderType(ch.getCardholderType());
            cardholder.setCardholderPhotoName(ch.getCardholderPhotoName());
            cardholder.setCardholderPhotoRRSKey(ch.getCardholderPhotoRRSKey());
            cardholder.setCardholderPhotoURL(ch.getCardholderPhotoURL());
            cardholder.setCardholderPhotoKey(ch.getCardholderPhotoKey());
            cardholderRepository.save(cardholder);
        }

        // Build response: travelcardId as UUID (external identifier), and a short token
        String travelcardUuid = UUID.randomUUID().toString();
        String token = generateToken();

        System.out.println("TravelcardService: exit createTravelcard travelcardDbId=" + saved.getId() + " travelcardUuid=" + travelcardUuid);

        return new CreateTravelcardResponse(travelcardUuid, token);
    }

    private String generateToken()
    {
        int length = 6;
        String chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        StringBuilder sb = new StringBuilder(length);
        ThreadLocalRandom rnd = ThreadLocalRandom.current();
        for (int i = 0; i < length; i++)
        {
            sb.append(chars.charAt(rnd.nextInt(chars.length())));
        }
        return sb.toString();
    }
}
