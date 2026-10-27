package com.ai2dev.bustravelaccounts.controller;

import com.ai2dev.bustravelaccounts.exception.BadRequestException;
import com.ai2dev.bustravelaccounts.exception.ConflictException;
import com.ai2dev.bustravelaccounts.exception.HotlistedAccountException;
import com.ai2dev.bustravelaccounts.exception.NotFoundException;
import com.ai2dev.bustravelaccounts.exception.UnknownErrorException;
import com.ai2dev.bustravelaccounts.model.error.ErrorResponse;
import java.time.OffsetDateTime;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
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
        return ResponseEntity.status(404).contentType(MediaType.APPLICATION_JSON).body(error(404, "RESOURCE NOT FOUND", "RESOURCE NOT FOUND"));
    }

    @ExceptionHandler(HttpRequestMethodNotSupportedException.class)
    public ResponseEntity<ErrorResponse> methodNotAllowed(Exception ex) {
        return ResponseEntity.status(405).contentType(MediaType.APPLICATION_JSON).body(error(405, "METHOD NOT ALLOWED", "METHOD NOT ALLOWED"));
    }

    @ExceptionHandler(HttpMediaTypeNotSupportedException.class)
    public ResponseEntity<ErrorResponse> unsupportedMediaType(Exception ex) {
        return ResponseEntity.status(415).contentType(MediaType.APPLICATION_JSON).body(error(415, "UNSUPPORTED MEDIA TYPE", "UNSUPPORTED MEDIA TYPE"));
    }

    @ExceptionHandler({MissingServletRequestParameterException.class, MethodArgumentTypeMismatchException.class, BadRequestException.class})
    public ResponseEntity<ErrorResponse> badRequest(Exception ex) {
        return ResponseEntity.status(400).contentType(MediaType.APPLICATION_JSON).body(error(400, "BAD REQUEST", "BAD REQUEST"));
    }

    @ExceptionHandler(ConflictException.class)
    public ResponseEntity<ErrorResponse> conflict(ConflictException ex) {
        return ResponseEntity.status(409).contentType(MediaType.APPLICATION_JSON).body(error(409, "CONFLICT", ex.getMessage()));
    }

    @ExceptionHandler(NotFoundException.class)
    public ResponseEntity<ErrorResponse> notFoundBusiness(NotFoundException ex) {
        return ResponseEntity.status(404).contentType(MediaType.APPLICATION_JSON).body(error(404, "RESOURCE NOT FOUND", ex.getMessage()));
    }

    @ExceptionHandler(HotlistedAccountException.class)
    public ResponseEntity<ErrorResponse> hotlisted(HotlistedAccountException ex) {
        return ResponseEntity.status(422).contentType(MediaType.APPLICATION_JSON).body(error(422, "UNPROCESSABLE ENTITY", ex.getMessage()));
    }

    @ExceptionHandler(UnknownErrorException.class)
    public ResponseEntity<ErrorResponse> unknown(UnknownErrorException ex) {
        return ResponseEntity.status(500).contentType(MediaType.APPLICATION_JSON).body(error(500, "MULE UNKNOWN ERROR", ex.getMessage()));
    }

    @ExceptionHandler(Exception.class)
    public ResponseEntity<ErrorResponse> unexpected(Exception ex) {
        log.error("Unexpected exception", ex);
        return ResponseEntity.status(500).contentType(MediaType.APPLICATION_JSON).body(error(500, "MULE UNKNOWN ERROR", "An unexpected error occurred"));
    }

    private ErrorResponse error(int code, String message, String description) {
        return new ErrorResponse(new ErrorResponse.ErrorBody(code, OffsetDateTime.now().toString(), message, description));
    }
}
