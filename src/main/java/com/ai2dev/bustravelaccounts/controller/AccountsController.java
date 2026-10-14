package com.ai2dev.bustravelaccounts.controller;

import com.ai2dev.bustravelaccounts.model.response.PaApiResult;
import com.ai2dev.bustravelaccounts.service.PaApiService;
import org.springframework.http.HttpHeaders;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.util.Map;

@RestController
public class AccountsController {
    private final PaApiService paApiService;

    public AccountsController(PaApiService paApiService) {
        this.paApiService = paApiService;
    }

    @GetMapping("/accounts")
    public ResponseEntity<String> getAccounts(@RequestParam(required = false) Map<String, String> query, @RequestHeader HttpHeaders headers) {
        PaApiResult result = paApiService.getAccounts(null, query == null ? Map.of() : query, headers.toSingleValueMap());
        return ResponseEntity.status(result.statusCode()).headers(h -> h.setAll(result.headers())).body(result.body());
    }

    @GetMapping("/accounts/{id}")
    public ResponseEntity<String> getAccountById(@PathVariable String id, @RequestHeader HttpHeaders headers) {
        PaApiResult result = paApiService.getAccountById(id, Map.of(), headers.toSingleValueMap());
        return ResponseEntity.status(result.statusCode()).headers(h -> h.setAll(result.headers())).body(result.body());
    }

    @PostMapping(value = "/accounts", consumes = "application/json")
    public ResponseEntity<String> postAccounts(@RequestBody(required = false) String body, @RequestHeader HttpHeaders headers) {
        PaApiResult result = paApiService.postAccounts(body, Map.of(), headers.toSingleValueMap());
        return ResponseEntity.status(result.statusCode()).headers(h -> h.setAll(result.headers())).body(result.body());
    }

    @PutMapping(value = "/accounts/{id}", consumes = "application/json")
    public ResponseEntity<String> putAccounts(@PathVariable String id, @RequestBody(required = false) String body, @RequestHeader HttpHeaders headers) {
        PaApiResult result = paApiService.putAccounts(id, body, Map.of(), headers.toSingleValueMap());
        return ResponseEntity.status(result.statusCode()).headers(h -> h.setAll(result.headers())).body(result.body());
    }

    @RequestMapping({"/alive", "/ready"})
    public ResponseEntity<String> health() {
        return ResponseEntity.ok("UP");
    }
}
