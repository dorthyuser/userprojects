package com.ai2dev.travelcardspringboot929.service;

import com.ai2dev.travelcardspringboot929.dto.CardholderRequestDto;
import com.ai2dev.travelcardspringboot929.dto.TravelcardRequestDto;
import com.ai2dev.travelcardspringboot929.dto.TravelcardResponseDto;
import com.ai2dev.travelcardspringboot929.model.CardholderEntity;
import com.ai2dev.travelcardspringboot929.model.CardholderType;
import com.ai2dev.travelcardspringboot929.model.TravelcardEntity;
import com.ai2dev.travelcardspringboot929.repository.CardholderRepository;
import com.ai2dev.travelcardspringboot929.repository.TravelcardRepository;
import java.util.UUID;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

@Service
public class TravelcardService {

    private static final Logger logger = LoggerFactory.getLogger(TravelcardService.class);

    private final TravelcardRepository travelcardRepository;
    private final CardholderRepository cardholderRepository;

    public TravelcardService(TravelcardRepository travelcardRepository, CardholderRepository cardholderRepository) {
        this.travelcardRepository = travelcardRepository;
        this.cardholderRepository = cardholderRepository;
    }

    @Transactional
    public TravelcardResponseDto createTravelcard(TravelcardRequestDto request, String clientId, String correlationId) {
        logger.info("business entry table=travelcards operation=INSERT client_id={} correlation_id={}", clientId, correlationId);
        TravelcardEntity savedTravelcard = travelcardRepository.save(new TravelcardEntity(
                null,
                request.travelcardType(),
                request.travelcardValidFrom(),
                request.travelcardValidTo(),
                request.travelcardName(),
                request.travelcardNumber(),
                request.travelcardRequestedDate(),
                request.travelcardTransactionReference(),
                request.travelcardUsableTo()));

        logger.info("db operation table=cardholders operation=INSERT client_id={} correlation_id={}", clientId, correlationId);
        for (CardholderRequestDto cardholderRequestDto : request.cardholders()) {
            cardholderRepository.save(new CardholderEntity(
                    null,
                    savedTravelcard.id(),
                    cardholderRequestDto.cardholderTitle(),
                    cardholderRequestDto.cardholderForename(),
                    cardholderRequestDto.cardholderSurname(),
                    cardholderRequestDto.cardholderType(),
                    cardholderRequestDto.cardholderPhotoName(),
                    cardholderRequestDto.cardholderPhotoRRSKey(),
                    cardholderRequestDto.cardholderPhotoURL(),
                    cardholderRequestDto.cardholderPhotoKey()));
        }

        String token = UUID.randomUUID().toString().substring(0, 6).toUpperCase();
        logger.info("business exit table=travelcards operation=INSERT travelcard_id={} client_id={} correlation_id={}", savedTravelcard.id(), clientId, correlationId);
        return new TravelcardResponseDto(String.valueOf(savedTravelcard.id()), token);
    }
}
