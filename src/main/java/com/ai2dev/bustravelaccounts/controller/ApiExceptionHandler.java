package com.ai2dev.bustravelaccounts.controller;

import com.ai2dev.bustravelaccounts.model.error.EaErrorBody;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.MissingServletRequestParameterException;
import org.springframework.web.bind.annotation.ExceptionHandler;
import org.springframework.web.bind.annotation.RestControllerAdvice;
import org.springframework.web.HttpMediaTypeNotAcceptableException;
import org.springframework.web.HttpMediaTypeNotSupportedException;
import org.springframework.web.HttpRequestMethodNotSupportedException;
import org.springframework.web.method.annotation.MethodArgumentTypeMismatchException;
import org.springframework.web.servlet.NoHandlerFoundException;
import org.springframework.web.servlet.resource.NoResourceFoundException;

import java.time.OffsetDateTime;

@RestControllerAdvice
public class ApiExceptionHandler {
    private static final Logger log = LoggerFactory.getLogger(ApiExceptionHandler.class);

    @ExceptionHandler({NoHandlerFoundException.class, NoResourceFoundException.class})
    public ResponseEntity<EaErrorBody> notFound(Exception e) {
        return ResponseEntity.status(HttpStatus.NOT_FOUND).body(new EaErrorBody(new EaErrorBody.Error(404, OffsetDateTime.now().toString(), "RESOURCE NOT FOUND", e.getMessage())));
    }

    @ExceptionHandler(HttpRequestMethodNotSupportedException.class)
    public ResponseEntity<EaErrorBody> methodNotAllowed(Exception e) {
        return ResponseEntity.status(HttpStatus.METHOD_NOT_ALLOWED).body(new EaErrorBody(new EaErrorBody.Error(405, OffsetDateTime.now().toString(), "METHOD NOT ALLOWED", e.getMessage())));
    }

    @ExceptionHandler(HttpMediaTypeNotAcceptableException.class)
    public ResponseEntity<EaErrorBody> notAcceptable(Exception e) {
        return ResponseEntity.status(HttpStatus.NOT_ACCEPTABLE).body(new EaErrorBody(new EaErrorBody.Error(406, OffsetDateTime.now().toString(), "NOT ACCEPTABLE", e.getMessage())));
    }

    @ExceptionHandler(HttpMediaTypeNotSupportedException.class)
    public ResponseEntity<EaErrorBody> unsupportedMediaType(Exception e) {
        return ResponseEntity.status(HttpStatus.UNSUPPORTED_MEDIA_TYPE).body(new EaErrorBody(new EaErrorBody.Error(415, OffsetDateTime.now().toString(), "UNSUPPORTED MEDIA TYPE", e.getMessage())));
    }

    @ExceptionHandler({MissingServletRequestParameterException.class, MethodArgumentTypeMismatchException.class})
    public ResponseEntity<EaErrorBody> badRequest(Exception e) {
        return ResponseEntity.status(HttpStatus.BAD_REQUEST).body(new EaErrorBody(new EaErrorBody.Error(400, OffsetDateTime.now().toString(), "BAD REQUEST", e.getMessage())));
    }

    @ExceptionHandler(Exception.class)
    public ResponseEntity<EaErrorBody> unexpected(Exception e) {
        log.error("Unexpected error", e);
        return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR).body(new EaErrorBody(new EaErrorBody.Error(500, OffsetDateTime.now().toString(), "MULE UNKNOWN ERROR", "An unexpected error occurred")));
    }
}
