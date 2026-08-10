package com.ai2dev.springbootae1019.exception;

public class EntityNotFoundException extends RuntimeException
{
    public EntityNotFoundException(String message)
    {
        super(message);
    }
}
