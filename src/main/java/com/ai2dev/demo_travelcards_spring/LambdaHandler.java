package com.ai2dev.demo_travelcards_spring;

import com.amazonaws.serverless.exceptions.ContainerInitializationException;
import com.amazonaws.serverless.proxy.model.AwsProxyRequest;
import com.amazonaws.serverless.proxy.model.AwsProxyResponse;
import com.amazonaws.serverless.proxy.spring.SpringBootLambdaContainerHandler;
import com.amazonaws.services.lambda.runtime.Context;
import com.amazonaws.services.lambda.runtime.RequestStreamHandler;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

public class LambdaHandler implements RequestStreamHandler {
    private static final Logger log = LoggerFactory.getLogger(LambdaHandler.class);

    private static SpringBootLambdaContainerHandler<AwsProxyRequest, AwsProxyResponse> handler;

    static {
        try {
            handler = SpringBootLambdaContainerHandler.getAwsProxyHandler(Application.class);
            log.info("SpringBootLambdaContainerHandler initialized");
        } catch (ContainerInitializationException e) {
            log.error("Spring Boot container init failed", e);
            throw new RuntimeException("Spring Boot container init failed", e);
        }
    }

    @Override
    public void handleRequest(InputStream in, OutputStream out, Context ctx) throws IOException {
        log.info("LambdaHandler.handleRequest entry");
        try {
            handler.proxyStream(in, out, ctx);
            log.info("LambdaHandler.handleRequest exit");
        } catch (RuntimeException e) {
            log.error("Error proxying stream", e);
            throw e;
        }
    }
}
