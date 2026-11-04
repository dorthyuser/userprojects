package com.ai2dev.odata4accounts.exception;

import com.ai2dev.odata4accounts.dto.error.ApiErrorBody;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.servlet.NoHandlerFoundException;
import org.springframework.web.bind.annotation.ExceptionHandler;
import org.springframework.web.bind.annotation.RestControllerAdvice;
import org.springframework.web.servlet.resource.NoResourceFoundException;

@RestControllerAdvice
public class ApiExceptionHandler {
    private static final Logger log = LoggerFactory.getLogger(ApiExceptionHandler.class);

    @ExceptionHandler(NoHandlerFoundException.class)
    public ResponseEntity<ApiErrorBody> handleNoHandlerFoundException(NoHandlerFoundException exception) {
        return ResponseEntity.status(HttpStatus.NOT_FOUND).body(new ApiErrorBody("UNKNOWN", "Not Found"));
    }

    @ExceptionHandler(NoResourceFoundException.class)
    public ResponseEntity<ApiErrorBody> handleNoResourceFoundException(NoResourceFoundException exception) {
        return ResponseEntity.status(HttpStatus.NOT_FOUND).body(new ApiErrorBody("UNKNOWN", "Not Found"));
    }

    @ExceptionHandler(Exception.class)
    public ResponseEntity<ApiErrorBody> handleException(Exception exception) {
        log.error("Unexpected exception", exception);
        return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR).body(new ApiErrorBody("UNKNOWN", "UNKNOWN"));
    }
}
