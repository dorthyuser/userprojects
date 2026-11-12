package com.ai2dev.bustravelaccountsea.controller;

import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping
public class HealthController {
    @GetMapping("/alive")
    public ResponseEntity<String> alive() {
        return ResponseEntity.ok("UP");
    }

    @GetMapping("/ready")
    public ResponseEntity<String> ready() {
        return ResponseEntity.ok("UP");
    }
}
