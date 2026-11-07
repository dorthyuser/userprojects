package com.ai2dev.bustravelaccountssa.exception;

import com.ai2dev.bustravelaccountssa.dto.error.ApiErrorResponse;
import java.time.OffsetDateTime;
import java.time.ZoneOffset;
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

    @ExceptionHandler({NoHandlerFoundException.class, NoResourceFoundException.class})
    public ResponseEntity<ApiErrorResponse> notFound(Exception ex) {
        return response(HttpStatus.NOT_FOUND, "RESOURCE NOT FOUND", ex);
    }

    @ExceptionHandler(HttpRequestMethodNotSupportedException.class)
    public ResponseEntity<ApiErrorResponse> methodNotAllowed(Exception ex) {
        return response(HttpStatus.METHOD_NOT_ALLOWED, "METHOD NOT ALLOWED", ex);
    }

    @ExceptionHandler(HttpMediaTypeNotAcceptableException.class)
    public ResponseEntity<ApiErrorResponse> notAcceptable(Exception ex) {
        return response(HttpStatus.NOT_ACCEPTABLE, "UNSUPPORTED MEDIA TYPE", ex);
    }

    @ExceptionHandler(HttpMediaTypeNotSupportedException.class)
    public ResponseEntity<ApiErrorResponse> unsupportedMediaType(Exception ex) {
        return response(HttpStatus.UNSUPPORTED_MEDIA_TYPE, "UNSUPPORTED MEDIA TYPE", ex);
    }

    @ExceptionHandler({HttpMessageNotReadableException.class, MissingServletRequestParameterException.class, MethodArgumentTypeMismatchException.class})
    public ResponseEntity<ApiErrorResponse> badRequest(Exception ex) {
        return response(HttpStatus.BAD_REQUEST, "BAD REQUEST", ex);
    }

    @ExceptionHandler(SalesforceException.class)
    public ResponseEntity<ApiErrorResponse> salesforce(SalesforceException ex) {
        HttpStatus status = switch (ex.errorType()) {
            case "SALESFORCE:INVALID_SESSION" -> HttpStatus.UNAUTHORIZED;
            case "SALESFORCE:NOT_FOUND" -> HttpStatus.NOT_FOUND;
            default -> HttpStatus.INTERNAL_SERVER_ERROR;
        };
        log.error("Salesforce error", ex);
        return ResponseEntity.status(status).contentType(MediaType.APPLICATION_JSON)
            .body(new ApiErrorResponse(new ApiErrorResponse.ErrorBody(status.value(), timestamp(), ex.errorType().replace(':', ' ') + " ERROR", ex.getMessage())));
    }

    @ExceptionHandler(Exception.class)
    public ResponseEntity<ApiErrorResponse> unexpected(Exception ex) {
        log.error("Unexpected error", ex);
        return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR)
            .contentType(MediaType.APPLICATION_JSON)
            .body(new ApiErrorResponse(new ApiErrorResponse.ErrorBody(500, timestamp(), "MULE UNKNOWN ERROR", "An unexpected error occurred")));
    }

    private ResponseEntity<ApiErrorResponse> response(HttpStatus status, String message, Exception ex) {
        log.warn("Handled error", ex);
        return ResponseEntity.status(status).contentType(MediaType.APPLICATION_JSON)
            .body(new ApiErrorResponse(new ApiErrorResponse.ErrorBody(status.value(), timestamp(), message, message)));
    }

    private String timestamp() { return OffsetDateTime.now(ZoneOffset.UTC).toString(); }
}
