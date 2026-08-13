package com.ai2dev.springboot1111.exception;

public class DuplicateEventException extends RuntimeException
{
    public DuplicateEventException(String message)
    {
        super(message);
    }
}
