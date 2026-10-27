package com.ai2dev.bustravelaccounts.controller;

import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

@RestController
public class HealthController {
    @RequestMapping("/alive")
    public ResponseEntity<String> alive() {
        return ResponseEntity.ok().contentType(MediaType.TEXT_PLAIN).body("UP");
    }

    @RequestMapping("/ready")
    public ResponseEntity<String> ready() {
        return ResponseEntity.ok().contentType(MediaType.TEXT_PLAIN).body("UP");
    }
}
