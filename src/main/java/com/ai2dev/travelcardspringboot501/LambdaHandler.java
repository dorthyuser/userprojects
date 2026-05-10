package com.ai2dev.travelcardspringboot501;

import com.amazonaws.serverless.exceptions.ContainerInitializationException;
import com.amazonaws.serverless.proxy.spring.SpringBootLambdaContainerHandler;
import com.amazonaws.services.lambda.runtime.Context;
import com.amazonaws.services.lambda.runtime.RequestStreamHandler;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

public class LambdaHandler implements RequestStreamHandler
{
    private static final Logger logger = LoggerFactory.getLogger(LambdaHandler.class);
    private static final SpringBootLambdaContainerHandler<?, ?> handler;

    static
    {
        try
        {
            handler = SpringBootLambdaContainerHandler.getAwsProxyHandler(Application.class);
        }
        catch (ContainerInitializationException e)
        {
            throw new RuntimeException("Failed to initialize Spring Boot Lambda handler", e);
        }
    }

    @Override
    public void handleRequest(InputStream inputStream, OutputStream outputStream, Context context) throws IOException
    {
        logger.info("Handling Lambda request");
        handler.proxyStream(inputStream, outputStream, context);
    }
}
