package com.ai2dev.bustravelaccountssa.exception;

import com.ai2dev.bustravelaccountssa.client.ErrorAuditS3Client;
import com.ai2dev.bustravelaccountssa.dto.error.ApiErrorResponse;
import jakarta.servlet.http.HttpServletRequest;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
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

    private final ErrorAuditS3Client errorAuditS3Client;

    public ApiExceptionHandler(ErrorAuditS3Client errorAuditS3Client) {
        this.errorAuditS3Client = errorAuditS3Client;
    }

    @ExceptionHandler({NoHandlerFoundException.class, NoResourceFoundException.class})
    public ResponseEntity<ApiErrorResponse> handleNotFound(Exception exception, HttpServletRequest request) {
        log.error("Error", exception);
        ApiErrorResponse body = ApiErrorResponse.of(404, "RESOURCE NOT FOUND", exception.getMessage());
        errorAuditS3Client.storeError(body, request.getMethod(), request.getRequestURI());
        return json(HttpStatus.NOT_FOUND, body);
    }

    @ExceptionHandler(HttpRequestMethodNotSupportedException.class)
    public ResponseEntity<ApiErrorResponse> handleMethodNotAllowed(HttpRequestMethodNotSupportedException exception, HttpServletRequest request) {
        log.error("Error", exception);
        ApiErrorResponse body = ApiErrorResponse.of(405, "METHOD NOT ALLOWED", exception.getMessage());
        errorAuditS3Client.storeError(body, request.getMethod(), request.getRequestURI());
        return json(HttpStatus.METHOD_NOT_ALLOWED, body);
    }

    @ExceptionHandler(HttpMediaTypeNotSupportedException.class)
    public ResponseEntity<ApiErrorResponse> handleUnsupportedMediaType(HttpMediaTypeNotSupportedException exception, HttpServletRequest request) {
        log.error("Error", exception);
        ApiErrorResponse body = ApiErrorResponse.of(415, "UNSUPPORTED MEDIA TYPE", exception.getMessage());
        errorAuditS3Client.storeError(body, request.getMethod(), request.getRequestURI());
        return json(HttpStatus.UNSUPPORTED_MEDIA_TYPE, body);
    }

    @ExceptionHandler(HttpMediaTypeNotAcceptableException.class)
    public ResponseEntity<ApiErrorResponse> handleNotAcceptable(HttpMediaTypeNotAcceptableException exception, HttpServletRequest request) {
        log.error("Error", exception);
        ApiErrorResponse body = ApiErrorResponse.of(500, "MULE UNKNOWN ERROR", "An unexpected error occurred");
        errorAuditS3Client.storeError(body, request.getMethod(), request.getRequestURI());
        return json(HttpStatus.INTERNAL_SERVER_ERROR, body);
    }

    @ExceptionHandler({HttpMessageNotReadableException.class, MissingServletRequestParameterException.class, MethodArgumentTypeMismatchException.class, BadRequestException.class})
    public ResponseEntity<ApiErrorResponse> handleBadRequest(Exception exception, HttpServletRequest request) {
        log.warn("BAD REQUEST decided for {} {}", request.getMethod(), request.getRequestURI(), exception);
        ApiErrorResponse body = ApiErrorResponse.of(400, "BAD REQUEST", exception.getMessage());
        errorAuditS3Client.storeError(body, request.getMethod(), request.getRequestURI());
        return json(HttpStatus.BAD_REQUEST, body);
    }

    @ExceptionHandler(SalesforceOperationException.class)
    public ResponseEntity<ApiErrorResponse> handleSalesforceOperation(SalesforceOperationException exception) {
        log.warn("Salesforce operation rejected", exception);
        return json(HttpStatus.BAD_REQUEST, exception.errorResponse());
    }

    @ExceptionHandler(SalesforceException.class)
    public ResponseEntity<ApiErrorResponse> handleSalesforce(SalesforceException exception, HttpServletRequest request) {
        log.error("Error", exception);
        if ("CONNECTIVITY".equals(exception.identifier()) && exception.getMessage() != null && exception.getMessage().contains("Invalid")) {
            ApiErrorResponse body = ApiErrorResponse.of(401, exception.namespace() + " " + exception.identifier() + " ERROR", exception.getMessage());
            errorAuditS3Client.storeError(body, request.getMethod(), request.getRequestURI());
            return json(HttpStatus.UNAUTHORIZED, body);
        }
        if ("CONNECTIVITY".equals(exception.identifier()) && exception.getMessage() != null && exception.getMessage().contains("Failed to send request")) {
            ApiErrorResponse body = ApiErrorResponse.of(404, exception.namespace() + " " + exception.identifier() + " ERROR", exception.getMessage());
            errorAuditS3Client.storeError(body, request.getMethod(), request.getRequestURI());
            return json(HttpStatus.NOT_FOUND, body);
        }
        ApiErrorResponse body = ApiErrorResponse.of(500, exception.namespace() + " " + exception.identifier() + " ERROR", exception.getMessage());
        errorAuditS3Client.storeError(body, request.getMethod(), request.getRequestURI());
        return json(HttpStatus.INTERNAL_SERVER_ERROR, body);
    }

    @ExceptionHandler(Exception.class)
    public ResponseEntity<ApiErrorResponse> handleAny(Exception exception, HttpServletRequest request) {
        log.error("Error", exception);
        ApiErrorResponse body = ApiErrorResponse.of(500, "MULE UNKNOWN ERROR", "An unexpected error occurred");
        errorAuditS3Client.storeError(body, request.getMethod(), request.getRequestURI());
        return json(HttpStatus.INTERNAL_SERVER_ERROR, body);
    }

    private static ResponseEntity<ApiErrorResponse> json(HttpStatus status, ApiErrorResponse body) {
        return ResponseEntity.status(status).contentType(MediaType.APPLICATION_JSON).body(body);
    }
}
