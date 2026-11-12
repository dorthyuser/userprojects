package com.ai2dev.bustravelaccountsea.exception;

import com.ai2dev.bustravelaccountsea.dto.error.ErrorResponse;
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

@RestControllerAdvice
public class ApiExceptionHandler {
    private static final Logger log = LoggerFactory.getLogger(ApiExceptionHandler.class);

    @ExceptionHandler({NoHandlerFoundException.class, NoResourceFoundException.class})
    public ResponseEntity<ErrorResponse> notFound(Exception e) {
        return ResponseEntity.status(HttpStatus.NOT_FOUND).contentType(MediaType.APPLICATION_JSON).body(new ErrorResponse(new ErrorResponse.Error(404, "RESOURCE NOT FOUND")));
    }

    @ExceptionHandler(HttpRequestMethodNotSupportedException.class)
    public ResponseEntity<ErrorResponse> methodNotAllowed(Exception e) {
        return ResponseEntity.status(HttpStatus.METHOD_NOT_ALLOWED).contentType(MediaType.APPLICATION_JSON).body(new ErrorResponse(new ErrorResponse.Error(405, "METHOD NOT ALLOWED")));
    }

    @ExceptionHandler(HttpMediaTypeNotSupportedException.class)
    public ResponseEntity<ErrorResponse> unsupportedMediaType(Exception e) {
        return ResponseEntity.status(HttpStatus.UNSUPPORTED_MEDIA_TYPE).contentType(MediaType.APPLICATION_JSON).body(new ErrorResponse(new ErrorResponse.Error(415, "UNSUPPORTED MEDIA TYPE")));
    }

    @ExceptionHandler({HttpMessageNotReadableException.class, MissingServletRequestParameterException.class, MethodArgumentTypeMismatchException.class})
    public ResponseEntity<ErrorResponse> badRequest(Exception e) {
        return ResponseEntity.status(HttpStatus.BAD_REQUEST).contentType(MediaType.APPLICATION_JSON).body(new ErrorResponse(new ErrorResponse.Error(400, "BAD REQUEST")));
    }

    @ExceptionHandler(MuleBusinessException.class)
    public ResponseEntity<ErrorResponse> muleBusiness(MuleBusinessException e) {
        log.error("Handled business error", e);
        return ResponseEntity.status(e.status()).contentType(MediaType.APPLICATION_JSON).body(new ErrorResponse(new ErrorResponse.Error(e.status().value(), e.message())));
    }

    @ExceptionHandler(Exception.class)
    public ResponseEntity<ErrorResponse> unexpected(Exception e) {
        log.error("Unexpected error", e);
        return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR).contentType(MediaType.APPLICATION_JSON).body(new ErrorResponse(new ErrorResponse.Error(500, "MULE UNKNOWN ERROR")));
    }
}
