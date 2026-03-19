package com.ai2dev.devninja_ai2dev_lambda;

import com.ai2dev.devninja_ai2dev_lambda.dto.CreateTravelcardRequest;
import com.ai2dev.devninja_ai2dev_lambda.dto.CreateTravelcardResponse;
import com.ai2dev.devninja_ai2dev_lambda.service.TravelcardService;
import com.amazonaws.services.lambda.runtime.events.APIGatewayProxyRequestEvent;
import com.amazonaws.services.lambda.runtime.events.APIGatewayProxyResponseEvent;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.datatype.jsr310.JavaTimeModule;
import io.micronaut.function.aws.MicronautRequestHandler;
import jakarta.inject.Inject;

public class LambdaHandler extends MicronautRequestHandler<APIGatewayProxyRequestEvent, APIGatewayProxyResponseEvent>
{
    @Inject
    private TravelcardService travelcardService;

    private final ObjectMapper objectMapper = new ObjectMapper()
            .registerModule(new JavaTimeModule());

    @Override
    public APIGatewayProxyResponseEvent execute(APIGatewayProxyRequestEvent input)
    {
        System.out.println("LambdaHandler: request received path=" + input.getPath());

        String clientId      = getHeader(input, "client_id");
        String correlationId = getHeader(input, "X-Correlation-Cust-Id");
        System.out.println("LambdaHandler: clientId=" + clientId + " correlationId=" + correlationId);

        APIGatewayProxyResponseEvent response = new APIGatewayProxyResponseEvent();
        try
        {
            CreateTravelcardRequest request = objectMapper.readValue(input.getBody(), CreateTravelcardRequest.class);
            CreateTravelcardResponse result = travelcardService.createTravelcard(request, clientId, correlationId);

            System.out.println("LambdaHandler: success travelcardId=" + result.getTravelcardId());
            response.setStatusCode(201);
            response.setBody(objectMapper.writeValueAsString(result));
        }
        catch (IllegalArgumentException e)
        {
            System.err.println("LambdaHandler: invalid input - " + e.getMessage());
            response.setStatusCode(400);
            response.setBody("{\"error\": \"Invalid input: " + e.getMessage() + "\"}");
        }
        catch (Exception e)
        {
            System.err.println("LambdaHandler: error - " + e.getMessage());
            response.setStatusCode(500);
            response.setBody("{\"error\": \"Internal server error\"}");
        }

        System.out.println("LambdaHandler: response status=" + response.getStatusCode());
        return response;
    }

    private String getHeader(APIGatewayProxyRequestEvent input, String name)
    {
        if (input.getHeaders() == null) return null;
        return input.getHeaders().entrySet().stream()
                .filter(e -> e.getKey().equalsIgnoreCase(name))
                .map(java.util.Map.Entry::getValue)
                .findFirst()
                .orElse(null);
    }
}
