package com.ai2dev.bustravelaccounts.controller;

import com.ai2dev.bustravelaccounts.exception.AccountNotFoundException;
import com.ai2dev.bustravelaccounts.exception.DuplicateAccountException;
import com.ai2dev.bustravelaccounts.exception.HotlistedAccountException;
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
    public ResponseEntity<ErrorResponse> notFound(Exception ex) {
        return error(HttpStatus.NOT_FOUND, "RESOURCE NOT FOUND", "RESOURCE NOT FOUND");
    }

    @ExceptionHandler(HttpRequestMethodNotSupportedException.class)
    public ResponseEntity<ErrorResponse> methodNotAllowed(Exception ex) {
        return error(HttpStatus.METHOD_NOT_ALLOWED, "METHOD NOT ALLOWED", "METHOD NOT ALLOWED");
    }

    @ExceptionHandler(HttpMediaTypeNotAcceptableException.class)
    public ResponseEntity<ErrorResponse> notAcceptable(Exception ex) {
        return error(HttpStatus.NOT_ACCEPTABLE, "UNSUPPORTED MEDIA TYPE", "UNSUPPORTED MEDIA TYPE");
    }

    @ExceptionHandler(HttpMediaTypeNotSupportedException.class)
    public ResponseEntity<ErrorResponse> unsupportedMediaType(Exception ex) {
        return error(HttpStatus.UNSUPPORTED_MEDIA_TYPE, "UNSUPPORTED MEDIA TYPE", "UNSUPPORTED MEDIA TYPE");
    }

    @ExceptionHandler({HttpMessageNotReadableException.class, MissingServletRequestParameterException.class, MethodArgumentTypeMismatchException.class})
    public ResponseEntity<ErrorResponse> badRequest(Exception ex) {
        return error(HttpStatus.BAD_REQUEST, "BAD REQUEST", "BAD REQUEST");
    }

    @ExceptionHandler(DuplicateAccountException.class)
    public ResponseEntity<ErrorResponse> duplicateAccount(DuplicateAccountException ex) {
        log.warn("Duplicate account request rejected", ex);
        return error(HttpStatus.CONFLICT, "CONFLICT", ex.getMessage());
    }

    @ExceptionHandler(AccountNotFoundException.class)
    public ResponseEntity<ErrorResponse> accountNotFound(AccountNotFoundException ex) {
        log.warn("Account not found", ex);
        return error(HttpStatus.NOT_FOUND, "RESOURCE NOT FOUND", ex.getMessage());
    }

    @ExceptionHandler(HotlistedAccountException.class)
    public ResponseEntity<ErrorResponse> hotlistedAccount(HotlistedAccountException ex) {
        log.warn("Hotlisted account cannot be changed", ex);
        return error(HttpStatus.UNPROCESSABLE_ENTITY, "UNPROCESSABLE ENTITY", ex.getMessage());
    }

    @ExceptionHandler(Exception.class)
    public ResponseEntity<ErrorResponse> unexpected(Exception ex) {
        log.error("Unexpected failure", ex);
        return error(HttpStatus.INTERNAL_SERVER_ERROR, "MULE UNKNOWN ERROR", "An unexpected error occurred");
    }

    private ResponseEntity<ErrorResponse> error(HttpStatus status, String message, String description) {
        ErrorResponse body = new ErrorResponse(new ErrorResponse.ErrorBody(status.value(), OffsetDateTime.now().toString(), message, description));
        return ResponseEntity.status(status).contentType(MediaType.APPLICATION_JSON).body(body);
    }
}
