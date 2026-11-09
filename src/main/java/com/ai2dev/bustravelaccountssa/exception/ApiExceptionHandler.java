package com.ai2dev.bustravelaccountssa.exception;

import com.ai2dev.bustravelaccountssa.dto.error.ApiErrorResponse;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.HttpHeaders;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.http.converter.HttpMessageNotReadableException;
import org.springframework.web.HttpMediaTypeNotAcceptableException;
import org.springframework.web.HttpMediaTypeNotSupportedException;
import org.springframework.web.HttpRequestMethodNotSupportedException;
import org.springframework.web.bind.MissingServletRequestParameterException;
import org.springframework.web.bind.annotation.ExceptionHandler;
import org.springframework.web.bind.annotation.RestControllerAdvice;
import org.springframework.web.method.annotation.MethodArgumentTypeMismatchException;
import org.springframework.web.servlet.NoHandlerFoundException;
import org.springframework.web.servlet.resource.NoResourceFoundException;

@RestControllerAdvice
public class ApiExceptionHandler {
    private static final Logger log = LoggerFactory.getLogger(ApiExceptionHandler.class);

    @ExceptionHandler({BadRequestException.class, HttpMessageNotReadableException.class, MissingServletRequestParameterException.class, MethodArgumentTypeMismatchException.class})
    public ResponseEntity<ApiErrorResponse> badRequest(Exception e) {
        log.warn("BAD REQUEST", e);
        return json(HttpStatus.BAD_REQUEST, ApiErrorResponse.of(400, "BAD REQUEST", "BAD REQUEST"));
    }

    @ExceptionHandler({NoHandlerFoundException.class, NoResourceFoundException.class})
    public ResponseEntity<ApiErrorResponse> notFound(Exception e) {
        log.error("RESOURCE NOT FOUND", e);
        return json(HttpStatus.NOT_FOUND, ApiErrorResponse.of(404, "RESOURCE NOT FOUND", "RESOURCE NOT FOUND"));
    }

    @ExceptionHandler(HttpRequestMethodNotSupportedException.class)
    public ResponseEntity<ApiErrorResponse> methodNotAllowed(HttpRequestMethodNotSupportedException e) {
        log.error("METHOD NOT ALLOWED", e);
        return json(HttpStatus.METHOD_NOT_ALLOWED, ApiErrorResponse.of(405, "METHOD NOT ALLOWED", "METHOD NOT ALLOWED"));
    }

    @ExceptionHandler(HttpMediaTypeNotSupportedException.class)
    public ResponseEntity<ApiErrorResponse> unsupportedMediaType(HttpMediaTypeNotSupportedException e) {
        log.error("UNSUPPORTED MEDIA TYPE", e);
        return json(HttpStatus.UNSUPPORTED_MEDIA_TYPE, ApiErrorResponse.of(415, "UNSUPPORTED MEDIA TYPE", "UNSUPPORTED MEDIA TYPE"));
    }

    @ExceptionHandler(HttpMediaTypeNotAcceptableException.class)
    public ResponseEntity<ApiErrorResponse> notAcceptable(HttpMediaTypeNotAcceptableException e) {
        log.error("NOT ACCEPTABLE", e);
        return json(HttpStatus.NOT_ACCEPTABLE, ApiErrorResponse.of(500, "MULE UNKNOWN ERROR", "An unexpected error occurred"));
    }

    @ExceptionHandler(SalesforceOperationException.class)
    public ResponseEntity<ApiErrorResponse> salesforceOperation(SalesforceOperationException e) {
        log.error("Salesforce operation failed", e);
        return json(e.status(), e.errorResponse());
    }

    @ExceptionHandler(SalesforceException.class)
    public ResponseEntity<ApiErrorResponse> salesforce(SalesforceException e) {
        log.error("Salesforce error", e);
        if ("CONNECTIVITY".equals(e.identifier()) && e.getMessage().contains("Invalid")) {
            return json(HttpStatus.UNAUTHORIZED, ApiErrorResponse.of(401, e.namespace() + " " + e.identifier() + " ERROR", e.getMessage()));
        }
        if ("CONNECTIVITY".equals(e.identifier()) && e.getMessage().contains("Failed to send request")) {
            return json(HttpStatus.NOT_FOUND, ApiErrorResponse.of(404, e.namespace() + " " + e.identifier() + " ERROR", e.getMessage()));
        }
        return json(HttpStatus.INTERNAL_SERVER_ERROR, ApiErrorResponse.of(500, e.namespace() + " " + e.identifier() + " ERROR", e.getMessage()));
    }

    @ExceptionHandler(Exception.class)
    public ResponseEntity<ApiErrorResponse> any(Exception e) {
        log.error("Unhandled error", e);
        return json(HttpStatus.INTERNAL_SERVER_ERROR, ApiErrorResponse.of(500, "MULE UNKNOWN ERROR", "An unexpected error occurred"));
    }

    private ResponseEntity<ApiErrorResponse> json(HttpStatus status, ApiErrorResponse body) {
        HttpHeaders headers = new HttpHeaders();
        headers.setContentType(MediaType.APPLICATION_JSON);
        return new ResponseEntity<>(body, headers, status);
    }
}
