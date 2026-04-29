package com.ai2dev.testsfgitpropjava.controller;

import com.ai2dev.testsfgitpropjava.model.AccountDto;
import com.ai2dev.testsfgitpropjava.service.AccountService;
import jakarta.validation.Valid;
import java.util.List;
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
@RequestMapping("/api/accounts")
public class AccountController
{
    private static final Logger logger = LoggerFactory.getLogger(AccountController.class);

    private final AccountService accountService;

    public AccountController(AccountService accountService)
    {
        this.accountService = accountService;
    }

    @PostMapping
    public ResponseEntity<AccountDto> create(@Valid @RequestBody AccountDto request)
    {
        logger.info("Entering create");
        AccountDto response = accountService.create(request);
        logger.info("Exiting create");
        return ResponseEntity.status(HttpStatus.CREATED).body(response);
    }

    @PutMapping("/{id}")
    public ResponseEntity<AccountDto> update(@PathVariable String id, @Valid @RequestBody AccountDto request)
    {
        logger.info("Entering update");
        AccountDto response = accountService.update(id, request);
        logger.info("Exiting update");
        return ResponseEntity.ok(response);
    }

    @GetMapping("/{id}")
    public ResponseEntity<AccountDto> getById(@PathVariable String id)
    {
        logger.info("Entering getById");
        AccountDto response = accountService.getById(id);
        logger.info("Exiting getById");
        return ResponseEntity.ok(response);
    }

    @GetMapping
    public ResponseEntity<List<AccountDto>> getAll()
    {
        logger.info("Entering getAll");
        List<AccountDto> response = accountService.getAll();
        logger.info("Exiting getAll");
        return ResponseEntity.ok(response);
    }

    @DeleteMapping("/{id}")
    public ResponseEntity<Void> delete(@PathVariable String id)
    {
        logger.info("Entering delete");
        accountService.delete(id);
        logger.info("Exiting delete");
        return ResponseEntity.noContent().build();
    }
}
