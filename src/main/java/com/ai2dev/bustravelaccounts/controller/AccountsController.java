package com.ai2dev.bustravelaccounts.controller;

import com.ai2dev.bustravelaccounts.model.response.PaApiResult;
import com.ai2dev.bustravelaccounts.service.EaService;
import java.util.Map;
import org.springframework.http.HttpHeaders;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.PutMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestHeader;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

@RestController
public class AccountsController {
    private final EaService service;

    public AccountsController(EaService service) {
        this.service = service;
    }

    @GetMapping("/accounts")
    public ResponseEntity<String> getAccounts(@RequestParam(required = false) String email, @RequestHeader HttpHeaders headers) {
        PaApiResult result = service.getAccounts(email, Map.of("email", email == null ? "" : email), headers.toSingleValueMap());
        return response(result);
    }

    @GetMapping("/accounts/{id}")
    public ResponseEntity<String> getAccountById(@PathVariable String id, @RequestHeader HttpHeaders headers) {
        PaApiResult result = service.getAccountById(id, Map.of(), headers.toSingleValueMap());
        return response(result);
    }

    @PostMapping(value = "/accounts", consumes = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<String> postAccounts(@RequestBody(required = false) String body, @RequestHeader HttpHeaders headers) {
        PaApiResult result = service.postAccounts(body, Map.of(), headers.toSingleValueMap());
        return response(result);
    }

    @PutMapping(value = "/accounts/{id}", consumes = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<String> putAccounts(@PathVariable String id, @RequestBody(required = false) String body, @RequestHeader HttpHeaders headers) {
        PaApiResult result = service.putAccounts(id, body, Map.of(), headers.toSingleValueMap());
        return response(result);
    }

    private ResponseEntity<String> response(PaApiResult result) {
        return ResponseEntity.status(result.statusCode()).headers(httpHeaders(result.headers())).body(result.body());
    }

    private HttpHeaders httpHeaders(Map<String, String> headers) {
        HttpHeaders httpHeaders = new HttpHeaders();
        headers.forEach(httpHeaders::add);
        if (!headers.containsKey("Content-Type")) {
            httpHeaders.setContentType(MediaType.APPLICATION_JSON);
        }
        return httpHeaders;
    }
}
