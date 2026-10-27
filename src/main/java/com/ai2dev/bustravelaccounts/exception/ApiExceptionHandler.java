package com.ai2dev.bustravelaccounts.exception;

import java.time.OffsetDateTime;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.http.converter.HttpMessageNotReadableException;
import org.springframework.web.HttpMediaTypeNotSupportedException;
import org.springframework.web.HttpRequestMethodNotSupportedException;
import org.springframework.web.bind.MissingServletRequestParameterException;
import org.springframework.web.bind.annotation.ExceptionHandler;
import org.springframework.web.bind.annotation.RestControllerAdvice;
import org.springframework.web.method.annotation.MethodArgumentTypeMismatchException;
import org.springframework.web.servlet.NoHandlerFoundException;
import org.springframework.web.servlet.resource.NoResourceFoundException;
import com.ai2dev.bustravelaccounts.dto.error.ErrorResponse;

@RestControllerAdvice
public class ApiExceptionHandler {
    private static final Logger log = LoggerFactory.getLogger(ApiExceptionHandler.class);

    @ExceptionHandler({NoHandlerFoundException.class, NoResourceFoundException.class})
    public ResponseEntity<ErrorResponse> notFound(Exception e) {
        return error(HttpStatus.NOT_FOUND, 404, "RESOURCE NOT FOUND", e);
    }

    @ExceptionHandler(HttpRequestMethodNotSupportedException.class)
    public ResponseEntity<ErrorResponse> methodNotAllowed(Exception e) {
        return error(HttpStatus.METHOD_NOT_ALLOWED, 405, "METHOD NOT ALLOWED", e);
    }

    @ExceptionHandler(HttpMediaTypeNotSupportedException.class)
    public ResponseEntity<ErrorResponse> unsupportedMediaType(Exception e) {
        return error(HttpStatus.UNSUPPORTED_MEDIA_TYPE, 415, "UNSUPPORTED MEDIA TYPE", e);
    }

    @ExceptionHandler({HttpMessageNotReadableException.class, MissingServletRequestParameterException.class, MethodArgumentTypeMismatchException.class, BadRequestException.class})
    public ResponseEntity<ErrorResponse> badRequest(Exception e) {
        String message = e instanceof BadRequestException ? e.getMessage() : "BAD REQUEST";
        return error(HttpStatus.BAD_REQUEST, 400, message, e);
    }

    @ExceptionHandler(ConflictException.class)
    public ResponseEntity<ErrorResponse> conflict(ConflictException e) { return error(HttpStatus.CONFLICT, 409, e.getMessage(), e); }

    @ExceptionHandler(NotFoundException.class)
    public ResponseEntity<ErrorResponse> notFoundBusiness(NotFoundException e) { return error(HttpStatus.NOT_FOUND, 404, e.getMessage(), e); }

    @ExceptionHandler(UnprocessableEntityException.class)
    public ResponseEntity<ErrorResponse> unprocessable(UnprocessableEntityException e) { return error(HttpStatus.UNPROCESSABLE_ENTITY, 422, e.getMessage(), e); }

    @ExceptionHandler(UnknownMuleException.class)
    public ResponseEntity<ErrorResponse> unknown(UnknownMuleException e) { return error(HttpStatus.INTERNAL_SERVER_ERROR, 500, "MULE UNKNOWN ERROR", e); }

    @ExceptionHandler(Exception.class)
    public ResponseEntity<ErrorResponse> unexpected(Exception e) {
        log.error("Unexpected error", e);
        return error(HttpStatus.INTERNAL_SERVER_ERROR, 500, "MULE UNKNOWN ERROR", e);
    }

    private ResponseEntity<ErrorResponse> error(HttpStatus status, int code, String message, Exception e) {
        log.error("Handled error", e);
        return ResponseEntity.status(status).contentType(MediaType.APPLICATION_JSON).body(new ErrorResponse(new ErrorResponse.ErrorBody(code, OffsetDateTime.now().toString(), message, message)));
    }
}
