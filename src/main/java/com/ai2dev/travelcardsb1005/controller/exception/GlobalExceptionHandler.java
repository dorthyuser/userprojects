package com.ai2dev.travelcardsb1005.exception;

import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.validation.FieldError;
import org.springframework.web.bind.MethodArgumentNotValidException;
import org.springframework.web.bind.annotation.ControllerAdvice;
import org.springframework.web.bind.annotation.ExceptionHandler;

@ControllerAdvice
public class GlobalExceptionHandler
{
    private static final Logger log = LoggerFactory.getLogger(GlobalExceptionHandler.class);

    @ExceptionHandler(MethodArgumentNotValidException.class)
    public ResponseEntity<Map<String, Object>> handleMethodArgumentNotValidException(MethodArgumentNotValidException ex)
    {
        Map<String, Object> body = new LinkedHashMap<>();
        List<Map<String, String>> errors = ex.getBindingResult().getFieldErrors().stream()
            .map(this::toError)
            .toList();
        body.put("errors", errors);
        log.error("validation_failed count={}", errors.size());
        return ResponseEntity.status(HttpStatus.BAD_REQUEST).body(body);
    }

    @ExceptionHandler(Exception.class)
    public ResponseEntity<Map<String, String>> handleException(Exception ex)
    {
        log.error("unhandled_exception message={}", ex.getMessage());
        Map<String, String> body = new LinkedHashMap<>();
        body.put("error", "Internal server error");
        return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR).body(body);
    }

    private Map<String, String> toError(FieldError fieldError)
    {
        Map<String, String> error = new LinkedHashMap<>();
        error.put("field", fieldError.getField());
        error.put("message", fieldError.getDefaultMessage());
        return error;
    }
}
