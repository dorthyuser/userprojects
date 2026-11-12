package com.ai2dev.bustravelaccountsea.controller;

import com.ai2dev.bustravelaccountsea.dto.response.AccountsResponse;
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
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping
public class AccountsController {
    private final AccountsService service;

    public AccountsController(AccountsService service) {
        this.service = service;
    }

    @GetMapping(value = "/accounts", produces = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<?> getAccounts(@RequestParam(required = false) String email,
                                         @RequestHeader HttpHeaders headers,
                                         HttpServletRequest request) {
        AccountsResponse response = service.getAccounts(email, headers, request);
        return ResponseEntity.status(response.status()).headers(response.headers()).body(response.body());
    }

    @GetMapping(value = "/accounts/{id}", produces = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<?> getAccount(@PathVariable String id,
                                        @RequestHeader HttpHeaders headers,
                                        HttpServletRequest request) {
        AccountsResponse response = service.getAccount(id, headers, request);
        return ResponseEntity.status(response.status()).headers(response.headers()).body(response.body());
    }

    @PostMapping(value = "/accounts", consumes = MediaType.APPLICATION_JSON_VALUE, produces = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<?> postAccounts(@RequestBody(required = false) String body,
                                          @RequestHeader HttpHeaders headers,
                                          HttpServletRequest request) {
        AccountsResponse response = service.postAccounts(body, headers, request);
        return ResponseEntity.status(response.status()).headers(response.headers()).body(response.body());
    }

    @PutMapping(value = "/accounts/{id}", consumes = MediaType.APPLICATION_JSON_VALUE, produces = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<?> putAccounts(@PathVariable String id,
                                         @RequestBody(required = false) String body,
                                         @RequestHeader HttpHeaders headers,
                                         HttpServletRequest request) {
        AccountsResponse response = service.putAccounts(id, body, headers, request);
        return ResponseEntity.status(response.status()).headers(response.headers()).body(response.body());
    }
}
