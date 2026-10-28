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
    private static final String PAYLOAD_ERROR_MESSAGE = "check payload existence or method/payload combination requirement";

    @ExceptionHandler({NoHandlerFoundException.class, NoResourceFoundException.class})
    public ResponseEntity<ErrorResponse> notFound(Exception ex) {
        return response(HttpStatus.NOT_FOUND, "RESOURCE NOT FOUND", ex);
    }

    @ExceptionHandler(HttpRequestMethodNotSupportedException.class)
    public ResponseEntity<ErrorResponse> methodNotAllowed(Exception ex) {
        return response(HttpStatus.METHOD_NOT_ALLOWED, "METHOD NOT ALLOWED", ex);
    }

    @ExceptionHandler(HttpMediaTypeNotAcceptableException.class)
    public ResponseEntity<ErrorResponse> notAcceptable(Exception ex) {
        return response(HttpStatus.NOT_ACCEPTABLE, "NOT ACCEPTABLE", ex);
    }

    @ExceptionHandler(HttpMediaTypeNotSupportedException.class)
    public ResponseEntity<ErrorResponse> unsupportedMediaType(Exception ex) {
        return response(HttpStatus.UNSUPPORTED_MEDIA_TYPE, "UNSUPPORTED MEDIA TYPE", ex);
    }

    @ExceptionHandler({HttpMessageNotReadableException.class, MissingServletRequestParameterException.class, MethodArgumentTypeMismatchException.class, BadRequestException.class})
    public ResponseEntity<ErrorResponse> badRequest(Exception ex) {
        String message = ex instanceof BadRequestException ? ex.getMessage() : "BAD REQUEST";
        return response(HttpStatus.BAD_REQUEST, message, ex);
    }

    @ExceptionHandler(ConflictException.class)
    public ResponseEntity<ErrorResponse> conflict(ConflictException ex) {
        return response(HttpStatus.CONFLICT, ex.getMessage(), ex);
    }

    @ExceptionHandler(NotFoundException.class)
    public ResponseEntity<ErrorResponse> notFoundBusiness(NotFoundException ex) {
        return response(HttpStatus.NOT_FOUND, ex.getMessage(), ex);
    }

    @ExceptionHandler(UnprocessableEntityException.class)
    public ResponseEntity<ErrorResponse> unprocessable(UnprocessableEntityException ex) {
        return response(HttpStatus.UNPROCESSABLE_ENTITY, ex.getMessage(), ex);
    }

    @ExceptionHandler(Exception.class)
    public ResponseEntity<ErrorResponse> unexpected(Exception ex) {
        log.error("Unexpected exception", ex);
        return response(HttpStatus.INTERNAL_SERVER_ERROR, "MULE UNKNOWN ERROR", ex);
    }

    private ResponseEntity<ErrorResponse> response(HttpStatus status, String message, Exception ex) {
        if (ex instanceof HttpMessageNotReadableException && ex.getMessage() != null && (ex.getMessage().contains("Java 8 optional type") || ex.getMessage().contains("Unable to parse empty input"))) {
            message = PAYLOAD_ERROR_MESSAGE;
        }
        if (!(ex instanceof ConflictException || ex instanceof NotFoundException || ex instanceof UnprocessableEntityException)) {
            log.error("Handled exception", ex);
        }
        ErrorResponse body = new ErrorResponse(new ErrorResponse.Error(status.value(), OffsetDateTime.now().toString(), message, message));
        return ResponseEntity.status(status).contentType(MediaType.APPLICATION_JSON).header(HttpHeaders.CONTENT_TYPE, MediaType.APPLICATION_JSON_VALUE).body(body);
    }
}
