package com.ai2dev.bustravelaccountssa.controller;

import java.time.OffsetDateTime;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.dao.DataAccessException;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.HttpMediaTypeNotSupportedException;
import org.springframework.web.HttpRequestMethodNotSupportedException;
import org.springframework.web.bind.MissingServletRequestParameterException;
import org.springframework.web.method.annotation.MethodArgumentTypeMismatchException;
import org.springframework.web.bind.annotation.ExceptionHandler;
import org.springframework.web.bind.annotation.RestControllerAdvice;
import org.springframework.web.servlet.NoHandlerFoundException;
import org.springframework.web.servlet.resource.NoResourceFoundException;

import com.ai2dev.bustravelaccountssa.exception.AccountNotFoundException;
import com.ai2dev.bustravelaccountssa.model.error.ErrorResponse;

@RestControllerAdvice
public class ApiExceptionHandler {
    private static final Logger log = LoggerFactory.getLogger(ApiExceptionHandler.class);

    @ExceptionHandler(NoHandlerFoundException.class)
    public ResponseEntity<ErrorResponse> notFound(Exception ex) {
        return error(HttpStatus.NOT_FOUND, 404, "RESOURCE NOT FOUND", ex);
    }

    @ExceptionHandler(NoResourceFoundException.class)
    public ResponseEntity<ErrorResponse> noResource(Exception ex) {
        return error(HttpStatus.NOT_FOUND, 404, "RESOURCE NOT FOUND", ex);
    }

    @ExceptionHandler(HttpRequestMethodNotSupportedException.class)
    public ResponseEntity<ErrorResponse> methodNotAllowed(Exception ex) {
        return error(HttpStatus.METHOD_NOT_ALLOWED, 405, "METHOD NOT ALLOWED", ex);
    }

    @ExceptionHandler(HttpMediaTypeNotSupportedException.class)
    public ResponseEntity<ErrorResponse> unsupportedMediaType(Exception ex) {
        return error(HttpStatus.UNSUPPORTED_MEDIA_TYPE, 415, "UNSUPPORTED MEDIA TYPE", ex);
    }

    @ExceptionHandler({MissingServletRequestParameterException.class, MethodArgumentTypeMismatchException.class})
    public ResponseEntity<ErrorResponse> badRequest(Exception ex) {
        return error(HttpStatus.BAD_REQUEST, 400, "BAD REQUEST", ex);
    }

    @ExceptionHandler(AccountNotFoundException.class)
    public ResponseEntity<ErrorResponse> accountNotFound(AccountNotFoundException ex) {
        log.warn("{}", ex.getMessage());
        return ResponseEntity.status(400).body(new ErrorResponse(new ErrorResponse.ErrorBody(400, OffsetDateTime.now(), "Account not found", ex.getMessage())));
    }

    @ExceptionHandler(DataAccessException.class)
    public ResponseEntity<ErrorResponse> dataAccess(DataAccessException ex) {
        Throwable cause = ex.getMostSpecificCause();
        String sqlState = cause instanceof java.sql.SQLException sql ? sql.getSQLState() : null;
        String message = sqlState == null ? "ERROR" : sqlState + " ERROR";
        log.error("Database operation failed", ex);
        return ResponseEntity.status(400).body(new ErrorResponse(new ErrorResponse.ErrorBody(400, OffsetDateTime.now(), message, cause == null ? "" : cause.getMessage())));
    }

    @ExceptionHandler(Exception.class)
    public ResponseEntity<ErrorResponse> unexpected(Exception ex) {
        log.error("Unexpected failure", ex);
        return ResponseEntity.status(500).body(new ErrorResponse(new ErrorResponse.ErrorBody(500, OffsetDateTime.now(), "MULE UNKNOWN ERROR", "An unexpected error occurred")));
    }

    private ResponseEntity<ErrorResponse> error(HttpStatus status, int code, String message, Exception ex) {
        log.warn("{}", ex.getMessage());
        return ResponseEntity.status(status).body(new ErrorResponse(new ErrorResponse.ErrorBody(code, OffsetDateTime.now(), message, ex.getMessage())));
    }
}
