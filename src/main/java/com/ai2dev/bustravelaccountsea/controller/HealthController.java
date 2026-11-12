package com.ai2dev.bustravelaccountsea.controller;

import com.ai2dev.bustravelaccountsea.service.HealthService;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping
public class HealthController {
    private final HealthService service;

    public HealthController(HealthService service) {
        this.service = service;
    }

    @GetMapping(value = "/alive", produces = MediaType.TEXT_PLAIN_VALUE)
    public ResponseEntity<String> alive() {
        return service.alive();
    }

    @GetMapping(value = "/ready", produces = MediaType.TEXT_PLAIN_VALUE)
    public ResponseEntity<String> ready() {
        return service.ready();
    }
}
