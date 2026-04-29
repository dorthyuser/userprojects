package com.ai2dev.testsfgitpropjava.service;

import com.ai2dev.testsfgitpropjava.model.AccountDto;
import java.util.List;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;

@Service
public class AccountService
{
    private static final Logger logger = LoggerFactory.getLogger(AccountService.class);

    public AccountDto create(AccountDto request)
    {
        logger.info("Entering create");
        logger.info("Exiting create");
        return request;
    }

    public AccountDto update(String id, AccountDto request)
    {
        logger.info("Entering update");
        logger.info("Exiting update");
        return new AccountDto(id, request.name(), request.type(), request.industry(), request.phone(), request.website(), request.email(), request.billingStreet(), request.billingCity(), request.billingState(), request.billingPostalCode(), request.billingCountry());
    }

    public AccountDto getById(String id)
    {
        logger.info("Entering getById");
        AccountDto response = new AccountDto(id, null, null, null, null, null, null, null, null, null, null, null);
        logger.info("Exiting getById");
        return response;
    }

    public List<AccountDto> getAll()
    {
        logger.info("Entering getAll");
        List<AccountDto> response = List.of();
        logger.info("Exiting getAll");
        return response;
    }

    public void delete(String id)
    {
        logger.info("Entering delete");
        logger.info("Exiting delete");
    }
}
