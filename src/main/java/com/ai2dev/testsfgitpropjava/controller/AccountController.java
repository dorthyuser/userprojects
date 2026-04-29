package com.ai2dev.testsfgitpropjava.controller;

import com.ai2dev.testsfgitpropjava.model.AccountDto;
import com.ai2dev.testsfgitpropjava.service.AccountService;
import jakarta.validation.Valid;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.HttpStatus;
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
    private static final Logger logger = LoggerFactory.getLogger(AccountController.class);

    private final AccountService accountService;

    public AccountController(AccountService accountService)
    {
        this.accountService = accountService;
    }

    @DeleteMapping("/{id}")
    public ResponseEntity<Void> deleteAccount(@PathVariable String id)
    {
        logger.info("Entering deleteAccount with id={}", id);
        accountService.deleteAccount(id);
        logger.info("Exiting deleteAccount with id={}", id);
        return ResponseEntity.noContent().build();
    }

    @GetMapping("/{id}")
    public ResponseEntity<AccountDto> getAccount(@PathVariable String id)
    {
        logger.info("Entering getAccount with id={}", id);
        AccountDto response = accountService.getAccount(id);
        logger.info("Exiting getAccount with id={}", id);
        return ResponseEntity.ok(response);
    }

    @PostMapping
    public ResponseEntity<AccountDto> createAccount(@Valid @RequestBody AccountDto request)
    {
        logger.info("Entering createAccount");
        AccountDto response = accountService.createAccount(request);
        logger.info("Exiting createAccount");
        return ResponseEntity.status(HttpStatus.CREATED).body(response);
    }

    @PutMapping("/{id}")
    public ResponseEntity<AccountDto> updateAccount(@PathVariable String id, @Valid @RequestBody AccountDto request)
    {
        logger.info("Entering updateAccount with id={}", id);
        AccountDto response = accountService.updateAccount(id, request);
        logger.info("Exiting updateAccount with id={}", id);
        return ResponseEntity.ok(response);
    }
}
