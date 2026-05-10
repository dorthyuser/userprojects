package com.ai2dev.travelcardsb1005;

import com.amazonaws.serverless.proxy.spring.SpringBootLambdaContainerHandler;
import com.amazonaws.services.lambda.runtime.Context;
import com.amazonaws.services.lambda.runtime.RequestStreamHandler;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;

public class LambdaHandler implements RequestStreamHandler
{
    private static final SpringBootLambdaContainerHandler<?, ?> handler;

    static
    {
        try
        {
            handler = SpringBootLambdaContainerHandler.getAwsProxyHandler(Application.class);
        }
        catch (Exception ex)
        {
            throw new RuntimeException("Failed to initialize Lambda handler", ex);
        }
    }

    @Override
    public void handleRequest(InputStream inputStream, OutputStream outputStream, Context context) throws IOException
    {
        handler.proxyStream(inputStream, outputStream, context);
    }
}
