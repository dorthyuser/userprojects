package com.ai2dev.bustravelaccountssa.controller;

import com.ai2dev.bustravelaccountssa.service.HealthService;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

@RestController
public class HealthController {
    private final HealthService healthService;

    public HealthController(HealthService healthService) {
        this.healthService = healthService;
    }

    @RequestMapping("/alive")
    public ResponseEntity<String> alive() {
        return healthService.alive();
    }

    @RequestMapping("/ready")
    public ResponseEntity<String> ready() {
        return healthService.ready();
    }
}
