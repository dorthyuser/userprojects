package com.ai2dev.testsfgitpropjava.service;

import com.ai2dev.testsfgitpropjava.model.AccountDto;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;

import java.util.Map;
import java.util.UUID;
import java.util.concurrent.ConcurrentHashMap;

@Service
public class AccountService
{
    private static final Logger logger = LoggerFactory.getLogger(AccountService.class);

    private final Map<String, AccountDto> accounts = new ConcurrentHashMap<>();

    public AccountDto createAccount(AccountDto request)
    {
        logger.info("Entering createAccount service");
        String id = UUID.randomUUID().toString();
        AccountDto response = new AccountDto(id, request.userId(), request.name(), request.email(), request.phone(), request.industry(), request.description());
        accounts.put(id, response);
        logger.info("Exiting createAccount service with id={}", id);
        return response;
    }

    public AccountDto getAccount(String id)
    {
        logger.info("Entering getAccount service with id={}", id);
        AccountDto response = accounts.get(id);
        if (response == null)
        {
            logger.error("Account not found with id={}", id);
            throw new AccountNotFoundException(id);
        }
        logger.info("Exiting getAccount service with id={}", id);
        return response;
    }

    public AccountDto updateAccount(String id, AccountDto request)
    {
        logger.info("Entering updateAccount service with id={}", id);
        if (!accounts.containsKey(id))
        {
            logger.error("Account not found with id={}", id);
            throw new AccountNotFoundException(id);
        }
        AccountDto response = new AccountDto(id, request.userId(), request.name(), request.email(), request.phone(), request.industry(), request.description());
        accounts.put(id, response);
        logger.info("Exiting updateAccount service with id={}", id);
        return response;
    }

    public void deleteAccount(String id)
    {
        logger.info("Entering deleteAccount service with id={}", id);
        AccountDto removed = accounts.remove(id);
        if (removed == null)
        {
            logger.error("Account not found with id={}", id);
            throw new AccountNotFoundException(id);
        }
        logger.info("Exiting deleteAccount service with id={}", id);
    }
}
