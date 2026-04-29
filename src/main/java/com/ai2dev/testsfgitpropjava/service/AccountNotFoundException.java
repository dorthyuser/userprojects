package com.ai2dev.testsfgitpropjava.service;

public class AccountNotFoundException extends RuntimeException
{
    public AccountNotFoundException(String id)
    {
        super("Account not found: " + id);
    }
}
