package com.ai2dev.bustravelaccountsea.controller;

import com.ai2dev.bustravelaccountsea.service.AccountsService;
import jakarta.servlet.http.HttpServletRequest;
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
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping
public class AccountsController {
    private final AccountsService service;

    public AccountsController(AccountsService service) {
        this.service = service;
    }

    @GetMapping("/accounts")
    public ResponseEntity<?> getAccounts(@RequestHeader HttpHeaders headers, HttpServletRequest request) {
        return service.getAccounts(headers, request.getQueryString());
    }

    @GetMapping("/accounts/{id}")
    public ResponseEntity<?> getAccount(@PathVariable String id, @RequestHeader HttpHeaders headers) {
        return service.getAccount(id, headers);
    }

    @PostMapping(value = "/accounts", consumes = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<?> postAccounts(@RequestBody(required = false) String body, @RequestHeader HttpHeaders headers) {
        return service.postAccounts(body, headers);
    }

    @PutMapping(value = "/accounts/{id}", consumes = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<?> putAccounts(@PathVariable String id, @RequestBody(required = false) String body, @RequestHeader HttpHeaders headers) {
        return service.putAccounts(id, body, headers);
    }
}
