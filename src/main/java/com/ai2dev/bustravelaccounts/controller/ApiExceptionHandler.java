package com.ai2dev.bustravelaccounts.controller;

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
import com.ai2dev.bustravelaccounts.exception.BadRequestException;
import com.ai2dev.bustravelaccounts.exception.ConflictException;
import com.ai2dev.bustravelaccounts.exception.DatabaseOperationException;
import com.ai2dev.bustravelaccounts.exception.MuleUnknownException;
import com.ai2dev.bustravelaccounts.exception.NotFoundException;
import com.ai2dev.bustravelaccounts.exception.UnprocessableEntityException;
import com.ai2dev.bustravelaccounts.model.error.ErrorResponse;

@RestControllerAdvice
public class ApiExceptionHandler {
    private static final Logger log = LoggerFactory.getLogger(ApiExceptionHandler.class);

    @ExceptionHandler({NoHandlerFoundException.class, NoResourceFoundException.class})
    public ResponseEntity<ErrorResponse> notFound(Exception ex) {
        return response(HttpStatus.NOT_FOUND, "RESOURCE NOT FOUND", "RESOURCE NOT FOUND");
    }

    @ExceptionHandler(HttpRequestMethodNotSupportedException.class)
    public ResponseEntity<ErrorResponse> methodNotAllowed(Exception ex) {
        return response(HttpStatus.METHOD_NOT_ALLOWED, "METHOD NOT ALLOWED", "METHOD NOT ALLOWED");
    }

    @ExceptionHandler(HttpMediaTypeNotAcceptableException.class)
    public ResponseEntity<ErrorResponse> notAcceptable(Exception ex) {
        return response(HttpStatus.NOT_ACCEPTABLE, "NOT ACCEPTABLE", "NOT ACCEPTABLE");
    }

    @ExceptionHandler(HttpMediaTypeNotSupportedException.class)
    public ResponseEntity<ErrorResponse> unsupportedMediaType(Exception ex) {
        return response(HttpStatus.UNSUPPORTED_MEDIA_TYPE, "UNSUPPORTED MEDIA TYPE", "UNSUPPORTED MEDIA TYPE");
    }

    @ExceptionHandler({HttpMessageNotReadableException.class, MissingServletRequestParameterException.class, MethodArgumentTypeMismatchException.class, BadRequestException.class})
    public ResponseEntity<ErrorResponse> badRequest(Exception ex) {
        return response(HttpStatus.BAD_REQUEST, "BAD REQUEST", "BAD REQUEST");
    }

    @ExceptionHandler(NotFoundException.class)
    public ResponseEntity<ErrorResponse> accountNotFound(NotFoundException ex) { return response(HttpStatus.NOT_FOUND, "RESOURCE NOT FOUND", ex.getMessage()); }

    @ExceptionHandler(UnprocessableEntityException.class)
    public ResponseEntity<ErrorResponse> unprocessable(UnprocessableEntityException ex) { return response(HttpStatus.UNPROCESSABLE_ENTITY, "UNPROCESSABLE ENTITY", ex.getMessage()); }

    @ExceptionHandler(ConflictException.class)
    public ResponseEntity<ErrorResponse> conflict(ConflictException ex) { return response(HttpStatus.CONFLICT, "CONFLICT", ex.getMessage()); }

    @ExceptionHandler(DatabaseOperationException.class)
    public ResponseEntity<ErrorResponse> database(DatabaseOperationException ex) { return response(HttpStatus.BAD_REQUEST, ex.getMessage(), ex.getMessage()); }

    @ExceptionHandler(MuleUnknownException.class)
    public ResponseEntity<ErrorResponse> muleUnknown(MuleUnknownException ex) { return response(HttpStatus.INTERNAL_SERVER_ERROR, "MULE UNKNOWN ERROR", ex.getMessage()); }

    @ExceptionHandler(Exception.class)
    public ResponseEntity<ErrorResponse> unexpected(Exception ex) {
        log.error("Unexpected exception", ex);
        return response(HttpStatus.INTERNAL_SERVER_ERROR, "MULE UNKNOWN ERROR", "An unexpected error occurred");
    }

    private ResponseEntity<ErrorResponse> response(HttpStatus status, String message, String description) {
        ErrorResponse body = new ErrorResponse(new ErrorResponse.Error(status.value(), OffsetDateTime.now().toString(), message, description));
        return ResponseEntity.status(status).contentType(MediaType.APPLICATION_JSON).body(body);
    }
}
