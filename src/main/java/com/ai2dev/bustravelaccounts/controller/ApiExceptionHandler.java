package com.ai2dev.bustravelaccounts.controller;

import com.ai2dev.bustravelaccounts.exception.BadRequestException;
import com.ai2dev.bustravelaccounts.exception.DatabaseOperationException;
import com.ai2dev.bustravelaccounts.exception.MuleUnknownErrorException;
import com.ai2dev.bustravelaccounts.exception.ResourceRuleException;
import com.ai2dev.bustravelaccounts.model.error.ErrorResponse;
import java.time.OffsetDateTime;
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
    public ResponseEntity<ErrorResponse> notFound(Exception exception) {
        log.error("Routing error", exception);
        return error(HttpStatus.NOT_FOUND, 404, "RESOURCE NOT FOUND", exception.getMessage());
    }

    @ExceptionHandler(HttpRequestMethodNotSupportedException.class)
    public ResponseEntity<ErrorResponse> methodNotAllowed(HttpRequestMethodNotSupportedException exception) {
        log.error("Method not allowed", exception);
        return error(HttpStatus.METHOD_NOT_ALLOWED, 405, "METHOD NOT ALLOWED", exception.getMessage());
    }

    @ExceptionHandler(HttpMediaTypeNotAcceptableException.class)
    public ResponseEntity<ErrorResponse> notAcceptable(HttpMediaTypeNotAcceptableException exception) {
        log.error("Not acceptable", exception);
        return unknownError("HTTP", "NOT_ACCEPTABLE", "An unexpected error occurred");
    }

    @ExceptionHandler(HttpMediaTypeNotSupportedException.class)
    public ResponseEntity<ErrorResponse> unsupportedMediaType(HttpMediaTypeNotSupportedException exception) {
        log.error("Unsupported media type", exception);
        return error(HttpStatus.UNSUPPORTED_MEDIA_TYPE, 415, "UNSUPPORTED MEDIA TYPE", exception.getMessage());
    }

    @ExceptionHandler({HttpMessageNotReadableException.class, MissingServletRequestParameterException.class, MethodArgumentTypeMismatchException.class, BadRequestException.class})
    public ResponseEntity<ErrorResponse> badRequest(Exception exception) {
        log.error("Bad request", exception);
        return error(HttpStatus.BAD_REQUEST, 400, "BAD REQUEST", exception.getMessage());
    }

    @ExceptionHandler(MuleUnknownErrorException.class)
    public ResponseEntity<ErrorResponse> muleUnknown(MuleUnknownErrorException exception) {
        log.error("Mule unknown error", exception);
        return error(HttpStatus.INTERNAL_SERVER_ERROR, 500, "MULE UNKNOWN ERROR", "check payload existence or method/payload combination requirement");
    }

    @ExceptionHandler(DatabaseOperationException.class)
    public ResponseEntity<ErrorResponse> databaseOperation(DatabaseOperationException exception) {
        log.error("Database operation failed", exception);
        return error(HttpStatus.BAD_REQUEST, 400, exception.errorMessage(), exception.errorDescription());
    }

    @ExceptionHandler(ResourceRuleException.class)
    public ResponseEntity<ErrorResponse> resourceRule(ResourceRuleException exception) {
        log.warn("Business rule response: {}", exception.getMessage());
        return error(HttpStatus.valueOf(exception.status()), exception.status(), exception.errorMessage(), exception.getMessage());
    }

    @ExceptionHandler(Exception.class)
    public ResponseEntity<ErrorResponse> catchAll(Exception exception) {
        log.error("Unexpected error", exception);
        String description = exception.getMessage();
        if (description != null && (description.contains("Java 8 optional type") || description.contains("Unable to parse empty input"))) {
            return error(HttpStatus.INTERNAL_SERVER_ERROR, 500, "MULE UNKNOWN ERROR", "check payload existence or method/payload combination requirement");
        }
        return unknownError("MULE", "UNKNOWN", "An unexpected error occurred");
    }

    private ResponseEntity<ErrorResponse> unknownError(String namespace, String identifier, String description) {
        return error(HttpStatus.INTERNAL_SERVER_ERROR, 500, namespace + " " + identifier + " ERROR", description);
    }

    private ResponseEntity<ErrorResponse> error(HttpStatus status, int errorCode, String errorMessage, String errorDescription) {
        ErrorResponse response = new ErrorResponse(errorCode, OffsetDateTime.now(), errorMessage, errorDescription);
        return ResponseEntity.status(status).contentType(MediaType.APPLICATION_JSON).body(response);
    }
}
