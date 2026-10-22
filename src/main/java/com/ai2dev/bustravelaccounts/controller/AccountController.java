package com.ai2dev.bustravelaccounts.controller;

import com.ai2dev.bustravelaccounts.model.request.AccountRequest;
import com.ai2dev.bustravelaccounts.model.response.AccountResponse;
import com.ai2dev.bustravelaccounts.model.response.IdResponse;
import com.ai2dev.bustravelaccounts.service.AccountService;
import com.ai2dev.bustravelaccounts.util.RequestUtil;
import java.util.List;
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
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping
public class AccountController {
    private final AccountService service;

    public AccountController(AccountService service) {
        this.service = service;
    }

    @GetMapping(value = "/accounts", produces = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<List<AccountResponse>> getAccounts(@RequestHeader HttpHeaders headers, @RequestParam(required = false) String email) {
        RequestUtil.correlationId(headers);
        return ResponseEntity.ok(service.getAccounts(email));
    }

    @GetMapping(value = "/accounts/{id}", produces = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<AccountResponse> getAccount(@RequestHeader HttpHeaders headers, @PathVariable String id) {
        RequestUtil.correlationId(headers);
        return ResponseEntity.ok(service.getAccount(id));
    }

    @PostMapping(value = "/accounts", consumes = MediaType.APPLICATION_JSON_VALUE, produces = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<IdResponse> create(@RequestHeader HttpHeaders headers, @RequestBody AccountRequest request) {
        RequestUtil.correlationId(headers);
        return ResponseEntity.status(201).body(service.create(request));
    }

    @PutMapping(value = "/accounts/{id}", consumes = MediaType.APPLICATION_JSON_VALUE, produces = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<IdResponse> update(@RequestHeader HttpHeaders headers, @PathVariable String id, @RequestBody AccountRequest request) {
        RequestUtil.correlationId(headers);
        return ResponseEntity.ok(service.update(id, request));
    }
}
