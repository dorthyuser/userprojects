package com.ai2dev.bustravelaccounts.controller;

import com.ai2dev.bustravelaccounts.exception.AccountHotlistedException;
import com.ai2dev.bustravelaccounts.exception.AccountNotFoundException;
import com.ai2dev.bustravelaccounts.exception.DuplicateAccountException;
import com.ai2dev.bustravelaccounts.model.error.ErrorResponse;
import java.time.OffsetDateTime;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.HttpStatus;
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
    public ResponseEntity<ErrorResponse> notFound(Exception e) {
        log.warn("Routing not found", e);
        return ResponseEntity.status(404).body(new ErrorResponse(404, OffsetDateTime.now(), "RESOURCE NOT FOUND", "Failed to find the resource"));
    }

    @ExceptionHandler(HttpRequestMethodNotSupportedException.class)
    public ResponseEntity<ErrorResponse> methodNotAllowed(Exception e) {
        log.warn("Routing method not allowed", e);
        return ResponseEntity.status(405).body(new ErrorResponse(405, OffsetDateTime.now(), "METHOD NOT ALLOWED", "Failed to process the request method"));
    }

    @ExceptionHandler(HttpMediaTypeNotAcceptableException.class)
    public ResponseEntity<ErrorResponse> notAcceptable(Exception e) {
        log.warn("Routing not acceptable", e);
        return ResponseEntity.status(406).body(new ErrorResponse(406, OffsetDateTime.now(), "NOT ACCEPTABLE", "Failed to process the request"));
    }

    @ExceptionHandler(HttpMediaTypeNotSupportedException.class)
    public ResponseEntity<ErrorResponse> unsupportedMediaType(Exception e) {
        log.warn("Routing unsupported media type", e);
        return ResponseEntity.status(415).body(new ErrorResponse(415, OffsetDateTime.now(), "UNSUPPORTED MEDIA TYPE", "Failed to process the request body"));
    }

    @ExceptionHandler({HttpMessageNotReadableException.class, MissingServletRequestParameterException.class, MethodArgumentTypeMismatchException.class})
    public ResponseEntity<ErrorResponse> badRequest(Exception e) {
        log.warn("Bad request", e);
        return ResponseEntity.status(400).body(new ErrorResponse(400, OffsetDateTime.now(), "BAD REQUEST", "Failed to read the request"));
    }

    @ExceptionHandler(DuplicateAccountException.class)
    public ResponseEntity<ErrorResponse> duplicate(DuplicateAccountException e) {
        log.warn("Duplicate account", e);
        return ResponseEntity.status(409).body(new ErrorResponse(409, OffsetDateTime.now(), "CONFLICT", "Failed to create the record"));
    }

    @ExceptionHandler(AccountNotFoundException.class)
    public ResponseEntity<ErrorResponse> accountNotFound(AccountNotFoundException e) {
        log.warn("Account not found", e);
        return ResponseEntity.status(404).body(new ErrorResponse(404, OffsetDateTime.now(), "RESOURCE NOT FOUND", "Failed to find the resource"));
    }

    @ExceptionHandler(AccountHotlistedException.class)
    public ResponseEntity<ErrorResponse> hotlisted(AccountHotlistedException e) {
        log.warn("Account hotlisted", e);
        return ResponseEntity.status(422).body(new ErrorResponse(422, OffsetDateTime.now(), "UNPROCESSABLE ENTITY", "Failed to update the record"));
    }

    @ExceptionHandler(Exception.class)
    public ResponseEntity<ErrorResponse> unexpected(Exception e) {
        log.error("Unexpected failure", e);
        return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR).body(new ErrorResponse(500, OffsetDateTime.now(), "MULE UNKNOWN ERROR", "An unexpected error occurred"));
    }
}
