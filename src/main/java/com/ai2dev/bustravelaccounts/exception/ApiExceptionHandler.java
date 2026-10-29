package com.ai2dev.bustravelaccounts.exception;

import com.ai2dev.bustravelaccounts.dto.error.ErrorResponse;
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

@RestControllerAdvice
public class ApiExceptionHandler {
    private static final Logger log = LoggerFactory.getLogger(ApiExceptionHandler.class);

    @ExceptionHandler({NoHandlerFoundException.class, NoResourceFoundException.class})
    public ResponseEntity<ErrorResponse> notFound(Exception ex) {
        return error(HttpStatus.NOT_FOUND, 404, "RESOURCE NOT FOUND", "RESOURCE NOT FOUND", ex);
    }

    @ExceptionHandler(HttpRequestMethodNotSupportedException.class)
    public ResponseEntity<ErrorResponse> methodNotAllowed(Exception ex) {
        return error(HttpStatus.METHOD_NOT_ALLOWED, 405, "METHOD NOT ALLOWED", "METHOD NOT ALLOWED", ex);
    }

    @ExceptionHandler(HttpMediaTypeNotAcceptableException.class)
    public ResponseEntity<ErrorResponse> notAcceptable(Exception ex) {
        return error(HttpStatus.NOT_ACCEPTABLE, 406, "NOT ACCEPTABLE", "NOT ACCEPTABLE", ex);
    }

    @ExceptionHandler(HttpMediaTypeNotSupportedException.class)
    public ResponseEntity<ErrorResponse> unsupportedMediaType(Exception ex) {
        return error(HttpStatus.UNSUPPORTED_MEDIA_TYPE, 415, "UNSUPPORTED MEDIA TYPE", "UNSUPPORTED MEDIA TYPE", ex);
    }

    @ExceptionHandler({HttpMessageNotReadableException.class, MissingServletRequestParameterException.class, MethodArgumentTypeMismatchException.class})
    public ResponseEntity<ErrorResponse> badRequest(Exception ex) {
        return error(HttpStatus.BAD_REQUEST, 400, "BAD REQUEST", "BAD REQUEST", ex);
    }

    @ExceptionHandler(DuplicateAccountException.class)
    public ResponseEntity<ErrorResponse> duplicate(DuplicateAccountException ex) {
        return error(HttpStatus.CONFLICT, 409, "CONFLICT", ex.getMessage(), ex);
    }

    @ExceptionHandler(AccountNotFoundException.class)
    public ResponseEntity<ErrorResponse> missing(AccountNotFoundException ex) {
        return error(HttpStatus.NOT_FOUND, 404, "RESOURCE NOT FOUND", ex.getMessage(), ex);
    }

    @ExceptionHandler(AccountHotlistedException.class)
    public ResponseEntity<ErrorResponse> hotlisted(AccountHotlistedException ex) {
        return error(HttpStatus.UNPROCESSABLE_ENTITY, 422, "UNPROCESSABLE ENTITY", ex.getMessage(), ex);
    }

    @ExceptionHandler(EmptyBodyException.class)
    public ResponseEntity<ErrorResponse> emptyBody(EmptyBodyException ex) {
        return error(HttpStatus.INTERNAL_SERVER_ERROR, 500, "MULE UNKNOWN ERROR", ex.getMessage(), ex);
    }

    @ExceptionHandler(Exception.class)
    public ResponseEntity<ErrorResponse> unexpected(Exception ex) {
        log.error("Unexpected error", ex);
        return error(HttpStatus.INTERNAL_SERVER_ERROR, 500, "MULE UNKNOWN ERROR", "An unexpected error occurred", ex);
    }

    private ResponseEntity<ErrorResponse> error(HttpStatus status, int code, String message, String description, Exception ex) {
        if (!(ex instanceof DuplicateAccountException || ex instanceof AccountNotFoundException || ex instanceof AccountHotlistedException || ex instanceof EmptyBodyException)) {
            log.error("Handled error", ex);
        }
        ErrorResponse body = new ErrorResponse(new ErrorResponse.ErrorBody(code, OffsetDateTime.now().toString(), message, description));
        return ResponseEntity.status(status).contentType(MediaType.APPLICATION_JSON).header(HttpHeaders.CONTENT_TYPE, MediaType.APPLICATION_JSON_VALUE).body(body);
    }
}
