package com.ai2dev.devninja_ai2dev_lambda;

import com.amazonaws.services.lambda.runtime.events.APIGatewayProxyRequestEvent;
import com.amazonaws.services.lambda.runtime.events.APIGatewayProxyResponseEvent;
import io.micronaut.function.aws.MicronautRequestHandler;

public class LambdaHandler extends MicronautRequestHandler<APIGatewayProxyRequestEvent, APIGatewayProxyResponseEvent>
{
    public LambdaHandler()
    {
        System.out.println("LambdaHandler: entering constructor");
    }

    @Override
    public APIGatewayProxyResponseEvent execute(APIGatewayProxyRequestEvent input)
    {
        System.out.println("LambdaHandler: request received");
        try
        {
            APIGatewayProxyResponseEvent result = super.handleRequest(input, null);
            System.out.println("LambdaHandler: request completed successfully");
            return result;
        }
        catch (RuntimeException e)
        {
            System.err.println("LambdaHandler: error processing request: " + e.getMessage());
            throw e;
        }
        catch (Exception e)
        {
            System.err.println("LambdaHandler: error processing request: " + e.getMessage());
            throw new RuntimeException(e);
        }
    }
}
