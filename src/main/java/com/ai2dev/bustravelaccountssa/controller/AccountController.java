package com.ai2dev.bustravelaccountssa.controller;

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

import com.ai2dev.bustravelaccountssa.model.request.AccountRequest;
import com.ai2dev.bustravelaccountssa.service.AccountService;

@RestController
@RequestMapping
public class AccountController {
    private final AccountService service;

    public AccountController(AccountService service) {
        this.service = service;
    }

    @GetMapping("/accounts")
    public ResponseEntity<String> list(@RequestParam(required = false) String email, @RequestHeader HttpHeaders headers) {
        var result = service.list(email, headers);
        return ResponseEntity.status(result.statusCode()).contentType(MediaType.APPLICATION_JSON).body(result.body());
    }

    @GetMapping("/accounts/{id}")
    public ResponseEntity<String> getById(@PathVariable String id, @RequestHeader HttpHeaders headers) {
        var result = service.getById(id, headers);
        return ResponseEntity.status(result.statusCode()).contentType(MediaType.APPLICATION_JSON).body(result.body());
    }

    @PostMapping(value = "/accounts", consumes = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<String> create(@RequestBody(required = false) AccountRequest body, @RequestHeader HttpHeaders headers) {
        var result = service.create(body, headers);
        return ResponseEntity.status(result.statusCode()).contentType(MediaType.APPLICATION_JSON).body(result.body());
    }

    @PutMapping(value = "/accounts/{id}", consumes = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<String> update(@PathVariable String id, @RequestBody(required = false) AccountRequest body, @RequestHeader HttpHeaders headers) {
        var result = service.update(id, body, headers);
        return ResponseEntity.status(result.statusCode()).contentType(MediaType.APPLICATION_JSON).body(result.body());
    }
}
