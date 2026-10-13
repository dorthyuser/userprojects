package com.ai2dev.bustravelaccountssa;

import com.amazonaws.serverless.proxy.RequestReader;
import com.amazonaws.serverless.proxy.model.AwsProxyRequestContext;
import com.amazonaws.services.lambda.runtime.Context;
import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import java.io.IOException;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.slf4j.MDC;
import org.springframework.stereotype.Component;
import org.springframework.web.filter.OncePerRequestFilter;

/**
 * Logs every answered request once (method, path, status, duration) and puts the correlation id and
 * the request ids of API Gateway and Lambda on every log line written while it runs. Never the query
 * string, headers or bodies: they can carry personal data and secrets.
 */
@Component
public class RequestLoggingFilter extends OncePerRequestFilter {

    private static final Logger log = LoggerFactory.getLogger(RequestLoggingFilter.class);

    @Override
    protected void doFilterInternal(HttpServletRequest request, HttpServletResponse response, FilterChain chain)
            throws ServletException, IOException {
        long started = System.nanoTime();
        String correlationId = request.getHeader("X-Correlation-ID");
        if (correlationId != null && !correlationId.isBlank()) {
            MDC.put("correlationId", correlationId);
        }
        if (request.getAttribute(RequestReader.API_GATEWAY_CONTEXT_PROPERTY) instanceof AwsProxyRequestContext gateway
                && gateway.getRequestId() != null) {
            MDC.put("apiRequestId", gateway.getRequestId());
        }
        if (request.getAttribute(RequestReader.LAMBDA_CONTEXT_PROPERTY) instanceof Context lambda) {
            MDC.put("awsRequestId", lambda.getAwsRequestId());
        }
        try {
            chain.doFilter(request, response);
        } finally {
            int status = response.getStatus();
            long millis = (System.nanoTime() - started) / 1_000_000;
            if (status >= 500) {
                log.error("{} {} answered {} in {} ms", request.getMethod(), request.getRequestURI(), status, millis);
            } else if (status >= 400) {
                log.warn("{} {} answered {} in {} ms", request.getMethod(), request.getRequestURI(), status, millis);
            } else {
                log.info("{} {} answered {} in {} ms", request.getMethod(), request.getRequestURI(), status, millis);
            }
            MDC.remove("correlationId");
            MDC.remove("apiRequestId");
            MDC.remove("awsRequestId");
        }
    }
}
