package com.ai2dev.bustravelaccounts.exception;

public class DatabaseOperationException extends RuntimeException {
    private final String sqlState;

    public DatabaseOperationException(String sqlState, String message) {
        super(message);
        this.sqlState = sqlState;
    }

    public String sqlState() { return sqlState; }
}
