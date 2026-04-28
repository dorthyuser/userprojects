package com.ai2dev.testsfgitpropjava.controller;

import com.ai2dev.testsfgitpropjava.model.AccountModel;
import com.ai2dev.testsfgitpropjava.service.AccountService;
import java.util.Map;
import jakarta.validation.Valid;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.DeleteMapping;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.PutMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("/accounts")
public class AccountController
{
    private static final Logger log = LoggerFactory.getLogger(AccountController.class);
    private final AccountService accountService;

    public AccountController(AccountService accountService)
    {
        this.accountService = accountService;
    }

    @PostMapping
    public ResponseEntity<AccountModel> create(@Valid @RequestBody AccountModel request)
    {
        log.info("Entering create endpoint");
        try
        {
            ResponseEntity<AccountModel> response = ResponseEntity.ok(accountService.create(request));
            log.info("Exiting create endpoint");
            return response;
        }
        catch (Exception ex)
        {
            log.error("Error in create endpoint", ex);
            throw ex;
        }
    }

    @GetMapping("/{id}")
    public ResponseEntity<AccountModel> retrieve(@PathVariable String id)
    {
        log.info("Entering retrieve endpoint");
        try
        {
            ResponseEntity<AccountModel> response = ResponseEntity.ok(accountService.retrieve(id));
            log.info("Exiting retrieve endpoint");
            return response;
        }
        catch (Exception ex)
        {
            log.error("Error in retrieve endpoint", ex);
            throw ex;
        }
    }

    @PutMapping("/{id}")
    public ResponseEntity<AccountModel> update(@PathVariable String id, @Valid @RequestBody AccountModel request)
    {
        log.info("Entering update endpoint");
        try
        {
            ResponseEntity<AccountModel> response = ResponseEntity.ok(accountService.update(id, request));
            log.info("Exiting update endpoint");
            return response;
        }
        catch (Exception ex)
        {
            log.error("Error in update endpoint", ex);
            throw ex;
        }
    }

    @DeleteMapping("/{id}")
    public ResponseEntity<Map<String, Object>> delete(@PathVariable String id)
    {
        log.info("Entering delete endpoint");
        try
        {
            ResponseEntity<Map<String, Object>> response = ResponseEntity.ok(accountService.delete(id));
            log.info("Exiting delete endpoint");
            return response;
        }
        catch (Exception ex)
        {
            log.error("Error in delete endpoint", ex);
            throw ex;
        }
    }
}
