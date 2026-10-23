package com.ai2dev.bustravelaccounts.controller;

import com.ai2dev.bustravelaccounts.service.HealthService;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RestController;

@RestController
public class HealthController {
    private final HealthService service;

    public HealthController(HealthService service) {
        this.service = service;
    }

    @GetMapping(value = "/alive", produces = MediaType.TEXT_PLAIN_VALUE)
    public ResponseEntity<String> alive() {
        return ResponseEntity.ok().contentType(MediaType.TEXT_PLAIN).body(service.alive());
    }

    @GetMapping(value = "/ready", produces = MediaType.TEXT_PLAIN_VALUE)
    public ResponseEntity<String> ready() {
        return ResponseEntity.ok().contentType(MediaType.TEXT_PLAIN).body(service.ready());
    }
}
