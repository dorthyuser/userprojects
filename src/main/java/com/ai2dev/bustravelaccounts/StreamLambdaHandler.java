package com.ai2dev.bustravelaccounts;

import com.ai2dev.bustravelaccounts.config.SnapStartHooks;
import com.amazonaws.serverless.exceptions.ContainerInitializationException;
import com.amazonaws.serverless.proxy.model.AwsProxyRequest;
import com.amazonaws.serverless.proxy.model.AwsProxyResponse;
import com.amazonaws.serverless.proxy.spring.SpringBootLambdaContainerHandler;
import com.amazonaws.services.lambda.runtime.Context;
import com.amazonaws.services.lambda.runtime.RequestStreamHandler;

import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;

/**
 * AWS Lambda entry point: every API Gateway REST API request (Lambda proxy
 * integration, payload format 1.0) runs through the Spring Boot application.
 * Spring starts with the "aws" profile (src/main/resources/application-aws.properties); with SnapStart
 * the started application is snapshotted (config/SnapStartHooks.java primes it first).
 */
public class StreamLambdaHandler implements RequestStreamHandler {

    private static final SpringBootLambdaContainerHandler<AwsProxyRequest, AwsProxyResponse> HANDLER;

    static {
        // No JVM DNS cache (AWS's SnapStart advice: Lambda caches DNS itself; a JVM cache taken into the snapshot
        // can point at old addresses after the restore). Before anything else, before any logger
        java.security.Security.setProperty("networkaddress.cache.ttl", "0");
        try {
            HANDLER = SpringBootLambdaContainerHandler.getAwsProxyHandler(Application.class, "aws");
            SnapStartHooks.register(HANDLER);
        } catch (ContainerInitializationException e) {
            throw new IllegalStateException("The Spring Boot application could not start", e);
        }
    }

    @Override
    public void handleRequest(InputStream input, OutputStream output, Context context) throws IOException {
        HANDLER.proxyStream(input, output, context);
    }
}
