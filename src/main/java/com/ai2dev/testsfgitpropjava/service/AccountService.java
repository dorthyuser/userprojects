package com.ai2dev.testsfgitpropjava.service;

import com.ai2dev.testsfgitpropjava.model.AccountModel;
import java.util.LinkedHashMap;
import java.util.Map;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.HttpStatus;
import org.springframework.stereotype.Service;
import org.springframework.web.server.ResponseStatusException;

@Service
public class AccountService
{
    private static final Logger log = LoggerFactory.getLogger(AccountService.class);

    public AccountModel create(AccountModel request)
    {
        log.info("Entering create");
        try
        {
            AccountModel response = new AccountModel(
                    "001000000000001",
                    request.name(),
                    request.phone(),
                    request.website(),
                    request.industry(),
                    request.description()
            );
            log.info("Exiting create");
            return response;
        }
        catch (Exception ex)
        {
            log.error("Error in create", ex);
            throw ex;
        }
    }

    public AccountModel retrieve(String id)
    {
        log.info("Entering retrieve");
        try
        {
            AccountModel response = new AccountModel(id, "Sample Account", "", "", "", "");
            log.info("Exiting retrieve");
            return response;
        }
        catch (Exception ex)
        {
            log.error("Error in retrieve", ex);
            throw ex;
        }
    }

    public AccountModel update(String id, AccountModel request)
    {
        log.info("Entering update");
        try
        {
            AccountModel response = new AccountModel(
                    id,
                    request.name(),
                    request.phone(),
                    request.website(),
                    request.industry(),
                    request.description()
            );
            log.info("Exiting update");
            return response;
        }
        catch (Exception ex)
        {
            log.error("Error in update", ex);
            throw ex;
        }
    }

    public Map<String, Object> delete(String id)
    {
        log.info("Entering delete");
        try
        {
            Map<String, Object> response = new LinkedHashMap<>();
            response.put("id", id);
            response.put("deleted", Boolean.TRUE);
            log.info("Exiting delete");
            return response;
        }
        catch (Exception ex)
        {
            log.error("Error in delete", ex);
            throw new ResponseStatusException(HttpStatus.INTERNAL_SERVER_ERROR, "Delete failed", ex);
        }
    }
}
