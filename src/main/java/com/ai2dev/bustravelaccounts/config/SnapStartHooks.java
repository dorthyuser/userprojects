package com.ai2dev.bustravelaccounts.config;

import com.amazonaws.serverless.proxy.model.AwsProxyRequest;
import com.amazonaws.serverless.proxy.model.AwsProxyResponse;
import com.amazonaws.serverless.proxy.spring.SpringBootLambdaContainerHandler;
import org.crac.Context;
import org.crac.Core;
import org.crac.Resource;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.io.ByteArrayInputStream;
import java.io.ByteArrayOutputStream;
import java.nio.charset.StandardCharsets;

/**
 * AWS Lambda SnapStart (published versions): Lambda starts the function once, takes a snapshot of it and
 * resumes every new instance from that snapshot instead of starting Spring again.
 * Before the snapshot one request runs through the application ("priming"), so Spring MVC, the JSON
 * mapping, the request filter and the error handling are loaded then, not on the first real request.
 * It asks for a path the API does not have: no controller runs, so no database, secret or other system
 * is called before the snapshot (beans are created on first use, after the restore).
 * Without SnapStart (a local run, the unpublished $LATEST) the hooks never run.
 */
public final class SnapStartHooks implements Resource {

    private static final Logger log = LoggerFactory.getLogger(SnapStartHooks.class);

    // An API Gateway REST API event (Lambda proxy integration, payload format 1.0)
    private static final String PRIMING_EVENT = """
            {"resource": "/{proxy+}", "path": "/snapstart-priming", "httpMethod": "GET",
             "headers": {"Accept": "application/json"}, "multiValueHeaders": {"Accept": ["application/json"]},
             "queryStringParameters": null, "multiValueQueryStringParameters": null,
             "pathParameters": {"proxy": "snapstart-priming"}, "stageVariables": null,
             "requestContext": {"resourcePath": "/{proxy+}", "httpMethod": "GET", "path": "/snapstart-priming",
                                "stage": "priming", "requestId": "snapstart-priming", "identity": {"sourceIp": "127.0.0.1"}},
             "body": null, "isBase64Encoded": false}
            """;

    // The global context keeps only a weak reference: this one keeps the hooks registered
    private static SnapStartHooks registered;

    private final SpringBootLambdaContainerHandler<AwsProxyRequest, AwsProxyResponse> handler;

    private SnapStartHooks(SpringBootLambdaContainerHandler<AwsProxyRequest, AwsProxyResponse> handler) {
        this.handler = handler;
    }

    public static synchronized void register(SpringBootLambdaContainerHandler<AwsProxyRequest, AwsProxyResponse> handler) {
        registered = new SnapStartHooks(handler);
        Core.getGlobalContext().register(registered);
    }

    @Override
    public void beforeCheckpoint(Context<? extends Resource> context) {
        // A failed priming never stops the snapshot: the first request then loads what it needs
        try {
            ByteArrayOutputStream response = new ByteArrayOutputStream();
            handler.proxyStream(new ByteArrayInputStream(PRIMING_EVENT.getBytes(StandardCharsets.UTF_8)), response, null);
            log.info("SnapStart priming done before the snapshot");
        } catch (Exception e) {
            log.warn("SnapStart priming failed: {}", e.toString());
        }
    }

    @Override
    public void afterRestore(Context<? extends Resource> context) {
        // Nothing to renew: connections, secrets and ids are made on first use, after the restore
    }
}
