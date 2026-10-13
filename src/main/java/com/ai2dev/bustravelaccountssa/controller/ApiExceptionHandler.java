package com.ai2dev.bustravelaccountssa.controller;

import java.time.OffsetDateTime;

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

import com.ai2dev.bustravelaccountssa.model.error.ErrorResponse;
import tools.jackson.databind.ObjectMapper;

@RestControllerAdvice
public class ApiExceptionHandler {
    private static final Logger log = LoggerFactory.getLogger(ApiExceptionHandler.class);
    private final ObjectMapper objectMapper;

    public ApiExceptionHandler(ObjectMapper objectMapper) {
        this.objectMapper = objectMapper;
    }

    @ExceptionHandler({NoHandlerFoundException.class, NoResourceFoundException.class})
    public ResponseEntity<String> notFound(Exception ex) throws Exception {
        return error(HttpStatus.NOT_FOUND, "RESOURCE NOT FOUND", "RESOURCE NOT FOUND");
    }

    @ExceptionHandler(HttpRequestMethodNotSupportedException.class)
    public ResponseEntity<String> methodNotAllowed(Exception ex) throws Exception {
        return error(HttpStatus.METHOD_NOT_ALLOWED, "METHOD NOT ALLOWED", "METHOD NOT ALLOWED");
    }

    @ExceptionHandler(HttpMediaTypeNotAcceptableException.class)
    public ResponseEntity<String> notAcceptable(Exception ex) throws Exception {
        return error(HttpStatus.NOT_ACCEPTABLE, "NOT ACCEPTABLE", "NOT ACCEPTABLE");
    }

    @ExceptionHandler(HttpMediaTypeNotSupportedException.class)
    public ResponseEntity<String> unsupportedMediaType(Exception ex) throws Exception {
        return error(HttpStatus.UNSUPPORTED_MEDIA_TYPE, "UNSUPPORTED MEDIA TYPE", "UNSUPPORTED MEDIA TYPE");
    }

    @ExceptionHandler({HttpMessageNotReadableException.class, MissingServletRequestParameterException.class, MethodArgumentTypeMismatchException.class})
    public ResponseEntity<String> badRequest(Exception ex) throws Exception {
        return error(HttpStatus.BAD_REQUEST, "BAD REQUEST", "BAD REQUEST");
    }

    @ExceptionHandler(Exception.class)
    public ResponseEntity<String> unexpected(Exception ex) throws Exception {
        log.error("Unexpected error", ex);
        return error(HttpStatus.INTERNAL_SERVER_ERROR, "MULE UNKNOWN ERROR", "An unexpected error occurred");
    }

    private ResponseEntity<String> error(HttpStatus status, String message, String description) throws Exception {
        String body = objectMapper.writeValueAsString(new ErrorResponse(new ErrorResponse.ErrorBody(status.value(), OffsetDateTime.now(), message, description)));
        return ResponseEntity.status(status).contentType(MediaType.APPLICATION_JSON).header(HttpHeaders.CONTENT_TYPE, MediaType.APPLICATION_JSON_VALUE).body(body);
    }
}
