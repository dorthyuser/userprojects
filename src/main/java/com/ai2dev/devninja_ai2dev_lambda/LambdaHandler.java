package com.ai2dev.devninja_ai2dev_lambda;

import com.ai2dev.devninja_ai2dev_lambda.dto.TravelcardRequest;
import com.ai2dev.devninja_ai2dev_lambda.dto.TravelcardResponse;
import com.ai2dev.devninja_ai2dev_lambda.exception.ApiException;
import com.ai2dev.devninja_ai2dev_lambda.service.TravelcardService;
import com.amazonaws.services.lambda.runtime.events.APIGatewayProxyRequestEvent;
import com.amazonaws.services.lambda.runtime.events.APIGatewayProxyResponseEvent;
import com.fasterxml.jackson.databind.ObjectMapper;
import io.micronaut.function.aws.MicronautRequestHandler;
import jakarta.inject.Inject;
import java.util.HashMap;
import java.util.Map;

public class LambdaHandler extends MicronautRequestHandler<APIGatewayProxyRequestEvent, APIGatewayProxyResponseEvent> {
    private static final org.slf4j.Logger LOG = org.slf4j.LoggerFactory.getLogger(LambdaHandler.class);

    @Inject
    TravelcardService travelcardService;

    @Inject
    ObjectMapper objectMapper;

    @Override
    public APIGatewayProxyResponseEvent execute(APIGatewayProxyRequestEvent input) {
        LOG.info("{\"event\":\"entry\"}");
        APIGatewayProxyResponseEvent resp = new APIGatewayProxyResponseEvent();
        try {
            // Validate headers
            Map<String, String> headers = input.getHeaders() == null ? new HashMap<>() : input.getHeaders();
            String clientId = headers.get("client_id");
            if (clientId == null || clientId.isBlank() || clientId.length() > 128) {
                throw new ApiException(400, "Invalid or missing client_id header");
            }
            String contentType = headers.get("Content-Type");
            if (input.getBody() != null && (contentType == null || !contentType.contains("application/json"))) {
                throw new ApiException(415, "Content-Type must be application/json when body is provided");
            }

            TravelcardRequest request = objectMapper.readValue(input.getBody(), TravelcardRequest.class);
            TravelcardResponse result = travelcardService.createTravelcard(request);

            resp.setStatusCode(201);
            resp.setBody(objectMapper.writeValueAsString(result));
            resp.setHeaders(Map.of("Content-Type", "application/json"));

            LOG.info("{\"event\":\"exit\",\"id\":\"{}\"}", result.getTravelcardId());
            return resp;
        } catch (ApiException e) {
            LOG.error("{\"event\":\"error\",\"message\":\"{}\"}", e.getMessage(), e);
            try {
                resp.setStatusCode(e.getStatusCode());
                resp.setBody(objectMapper.writeValueAsString(Map.of("error", e.getMessage())));
                resp.setHeaders(Map.of("Content-Type", "application/json"));
            } catch (Exception ex) {
                LOG.error("{\"event\":\"error\",\"message\":\"{}\"}", ex.getMessage(), ex);
                resp.setStatusCode(500);
                resp.setBody("{\"error\":\"internal error\"}");
            }
            return resp;
        } catch (Exception e) {
            LOG.error("{\"event\":\"error\",\"message\":\"{}\"}", e.getMessage(), e);
            try {
                resp.setStatusCode(500);
                resp.setBody(objectMapper.writeValueAsString(Map.of("error", "internal error")));
                resp.setHeaders(Map.of("Content-Type", "application/json"));
            } catch (Exception ex) {
                LOG.error("{\"event\":\"error\",\"message\":\"{}\"}", ex.getMessage(), ex);
                resp.setStatusCode(500);
                resp.setBody("{\"error\":\"internal error\"}");
            }
            return resp;
        }
    }
}
