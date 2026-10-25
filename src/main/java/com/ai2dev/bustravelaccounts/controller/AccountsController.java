package com.ai2dev.bustravelaccounts.controller;

import com.ai2dev.bustravelaccounts.service.AccountsService;
import org.springframework.http.HttpHeaders;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.PutMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestHeader;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestMethod;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

@RestController
public class AccountsController {
    private final AccountsService accountsService;

    public AccountsController(AccountsService accountsService) {
        this.accountsService = accountsService;
    }

    @GetMapping("/accounts")
    public ResponseEntity<?> getAccounts(@RequestParam(required = false) String email, @RequestHeader HttpHeaders headers) {
        return accountsService.getAccounts(email, headers);
    }

    @GetMapping("/accounts/{id}")
    public ResponseEntity<?> getAccount(@PathVariable String id, @RequestHeader HttpHeaders headers) {
        return accountsService.getAccount(id, headers);
    }

    @PostMapping(value = "/accounts", consumes = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<?> createAccount(@RequestBody(required = false) String body, @RequestHeader HttpHeaders headers) {
        return accountsService.createAccount(body, headers);
    }

    @PutMapping(value = "/accounts/{id}", consumes = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<?> updateAccount(@PathVariable String id, @RequestBody(required = false) String body, @RequestHeader HttpHeaders headers) {
        return accountsService.updateAccount(id, body, headers);
    }

    @RequestMapping(value = "/alive", method = {RequestMethod.GET, RequestMethod.POST, RequestMethod.PUT, RequestMethod.DELETE, RequestMethod.PATCH, RequestMethod.OPTIONS, RequestMethod.HEAD})
    public ResponseEntity<String> alive() {
        return ResponseEntity.ok().contentType(MediaType.TEXT_PLAIN).body("UP");
    }

    @RequestMapping(value = "/ready", method = {RequestMethod.GET, RequestMethod.POST, RequestMethod.PUT, RequestMethod.DELETE, RequestMethod.PATCH, RequestMethod.OPTIONS, RequestMethod.HEAD})
    public ResponseEntity<String> ready() {
        return ResponseEntity.ok().contentType(MediaType.TEXT_PLAIN).body("UP");
    }
}
