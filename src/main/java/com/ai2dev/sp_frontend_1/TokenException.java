package com.ai2dev.sp_frontend_1;

/**
 * Exception thrown when token generation fails.
 */
public class TokenException extends RuntimeException {
    public TokenException(String message) {
        super(message);
    }

    public TokenException(String message, Throwable cause) {
        super(message, cause);
    }
}
