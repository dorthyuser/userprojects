package com.ai2dev.bustravelaccounts.controller;

import com.ai2dev.bustravelaccounts.exception.AccountHotlistedException;
import com.ai2dev.bustravelaccounts.exception.AccountNotFoundException;
import com.ai2dev.bustravelaccounts.exception.BadRequestException;
import com.ai2dev.bustravelaccounts.exception.DatabaseOperationException;
import com.ai2dev.bustravelaccounts.exception.DuplicateEmailException;
import com.ai2dev.bustravelaccounts.exception.MuleUnknownException;
import com.ai2dev.bustravelaccounts.model.error.ErrorResponse;
import java.time.OffsetDateTime;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.dao.DataAccessException;
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
    public ResponseEntity<ErrorResponse> handleNotFound(Exception ex) {
        log.error("Routing error", ex);
        return json(HttpStatus.NOT_FOUND, "RESOURCE NOT FOUND", "RESOURCE NOT FOUND");
    }

    @ExceptionHandler(HttpRequestMethodNotSupportedException.class)
    public ResponseEntity<ErrorResponse> handleMethodNotAllowed(HttpRequestMethodNotSupportedException ex) {
        log.error("Method not allowed", ex);
        return json(HttpStatus.METHOD_NOT_ALLOWED, "METHOD NOT ALLOWED", "METHOD NOT ALLOWED");
    }

    @ExceptionHandler(HttpMediaTypeNotSupportedException.class)
    public ResponseEntity<ErrorResponse> handleUnsupportedMediaType(HttpMediaTypeNotSupportedException ex) {
        log.error("Unsupported media type", ex);
        return json(HttpStatus.UNSUPPORTED_MEDIA_TYPE, "UNSUPPORTED MEDIA TYPE", "UNSUPPORTED MEDIA TYPE");
    }

    @ExceptionHandler(HttpMediaTypeNotAcceptableException.class)
    public ResponseEntity<ErrorResponse> handleNotAcceptable(HttpMediaTypeNotAcceptableException ex) {
        log.error("Not acceptable", ex);
        return unknownError("MULE", "UNKNOWN", "An unexpected error occurred");
    }

    @ExceptionHandler({HttpMessageNotReadableException.class, MissingServletRequestParameterException.class, MethodArgumentTypeMismatchException.class, BadRequestException.class})
    public ResponseEntity<ErrorResponse> handleBadRequest(Exception ex) {
        log.error("Bad request", ex);
        return json(HttpStatus.BAD_REQUEST, "BAD REQUEST", "BAD REQUEST");
    }

    @ExceptionHandler(MuleUnknownException.class)
    public ResponseEntity<ErrorResponse> handleMuleUnknown(MuleUnknownException ex) {
        log.error("Mule unknown error", ex);
        return json(HttpStatus.INTERNAL_SERVER_ERROR, "MULE UNKNOWN ERROR", "check payload existence or method/payload combination requirement");
    }

    @ExceptionHandler(DuplicateEmailException.class)
    public ResponseEntity<ErrorResponse> handleDuplicateEmail(DuplicateEmailException ex) {
        log.error("Duplicate email", ex);
        return json(HttpStatus.CONFLICT, "CONFLICT", ex.getMessage());
    }

    @ExceptionHandler(AccountNotFoundException.class)
    public ResponseEntity<ErrorResponse> handleAccountNotFound(AccountNotFoundException ex) {
        log.error("Account not found", ex);
        return json(HttpStatus.NOT_FOUND, "RESOURCE NOT FOUND", ex.getMessage());
    }

    @ExceptionHandler(AccountHotlistedException.class)
    public ResponseEntity<ErrorResponse> handleAccountHotlisted(AccountHotlistedException ex) {
        log.error("Account hotlisted", ex);
        return json(HttpStatus.UNPROCESSABLE_ENTITY, "UNPROCESSABLE ENTITY", ex.getMessage());
    }

    @ExceptionHandler(DatabaseOperationException.class)
    public ResponseEntity<ErrorResponse> handleDatabaseOperation(DatabaseOperationException ex) {
        log.error("Database operation failed", ex);
        return json(HttpStatus.BAD_REQUEST, ex.errorMessage(), ex.errorDescription());
    }

    @ExceptionHandler(DataAccessException.class)
    public ResponseEntity<ErrorResponse> handleDataAccess(DataAccessException ex) {
        log.error("Unexpected database access error", ex);
        return unknownError("MULE", "UNKNOWN", "An unexpected error occurred");
    }

    @ExceptionHandler(IllegalArgumentException.class)
    public ResponseEntity<ErrorResponse> handleIllegalArgument(IllegalArgumentException ex) {
        log.error("Unexpected illegal argument", ex);
        String description = ex.getMessage();
        if (description != null && (description.contains("Java 8 optional type") || description.contains("Unable to parse empty input"))) {
            return json(HttpStatus.INTERNAL_SERVER_ERROR, "MULE UNKNOWN ERROR", "check payload existence or method/payload combination requirement");
        }
        return unknownError("MULE", "UNKNOWN", "An unexpected error occurred");
    }

    @ExceptionHandler(Exception.class)
    public ResponseEntity<ErrorResponse> handleAny(Exception ex) {
        log.error("Unexpected error", ex);
        String description = ex.getMessage();
        if (description != null && (description.contains("Java 8 optional type") || description.contains("Unable to parse empty input"))) {
            return json(HttpStatus.INTERNAL_SERVER_ERROR, "MULE UNKNOWN ERROR", "check payload existence or method/payload combination requirement");
        }
        return unknownError("MULE", "UNKNOWN", "An unexpected error occurred");
    }

    private ResponseEntity<ErrorResponse> unknownError(String namespace, String identifier, String description) {
        return json(HttpStatus.INTERNAL_SERVER_ERROR, namespace + " " + identifier + " ERROR", description);
    }

    private ResponseEntity<ErrorResponse> json(HttpStatus status, String message, String description) {
        ErrorResponse body = new ErrorResponse(status.value(), OffsetDateTime.now(), message, description);
        return ResponseEntity.status(status).contentType(MediaType.APPLICATION_JSON).body(body);
    }
}
