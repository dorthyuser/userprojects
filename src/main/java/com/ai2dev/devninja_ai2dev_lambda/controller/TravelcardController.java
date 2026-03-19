package com.ai2dev.devninja_ai2dev_lambda.controller;

import com.ai2dev.devninja_ai2dev_lambda.dto.CardholderDto;
import com.ai2dev.devninja_ai2dev_lambda.dto.CreateTravelcardRequest;
import com.ai2dev.devninja_ai2dev_lambda.dto.CreateTravelcardResponse;
import com.ai2dev.devninja_ai2dev_lambda.service.TravelcardService;
import jakarta.inject.Inject;
import jakarta.validation.Valid;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Size;
import java.util.List;
import java.util.Objects;
import io.micronaut.http.HttpHeaders;
import io.micronaut.http.HttpResponse;
import io.micronaut.http.MediaType;
import io.micronaut.http.annotation.Controller;
import io.micronaut.http.annotation.Header;
import io.micronaut.http.annotation.Post;

@Controller("/travelcards")
public class TravelcardController
{
    @Inject
    TravelcardService travelcardService;

    @Post(consumes = MediaType.APPLICATION_JSON, produces = MediaType.APPLICATION_JSON)
    public HttpResponse<CreateTravelcardResponse> createTravelcard(
            @Header("client_id") @NotBlank @Size(min = 1, max = 128) String clientId,
            @Header(HttpHeaders.CONTENT_TYPE) String contentType,
            @Header("X-Correlation-Cust-Id") @Size(max = 100) String correlationId,
            @Valid CreateTravelcardRequest request)
    {
        System.out.println("TravelcardController: enter createTravelcard clientId=" + clientId + " correlationId=" + correlationId);

        if (contentType == null || !contentType.toLowerCase().contains(MediaType.APPLICATION_JSON))
        {
            System.err.println("TravelcardController: invalid content-type=" + contentType);
            return HttpResponse.badRequest();
        }

        CreateTravelcardResponse response = travelcardService.createTravelcard(request);

        System.out.println("TravelcardController: exit createTravelcard travelcardId=" + response.getTravelcardId());
        return HttpResponse.created(response);
    }
}
