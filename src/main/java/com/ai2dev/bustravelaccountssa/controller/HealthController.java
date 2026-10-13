package com.ai2dev.bustravelaccountssa.controller;

import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import com.ai2dev.bustravelaccountssa.service.AccountService;

@RestController
public class HealthController {
    private final AccountService service;

    public HealthController(AccountService service) {
        this.service = service;
    }

    @RequestMapping("/alive")
    public ResponseEntity<String> alive() {
        return ResponseEntity.ok("UP");
    }

    @RequestMapping("/ready")
    public ResponseEntity<String> ready() {
        return service.ready() ? ResponseEntity.ok("UP") : ResponseEntity.status(500).body("DOWN");
    }
}
