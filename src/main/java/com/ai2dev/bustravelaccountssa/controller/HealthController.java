package com.ai2dev.bustravelaccountssa.controller;

import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping
public class HealthController {
    @GetMapping("/alive")
    public ResponseEntity<String> alive() {
        return ResponseEntity.ok().contentType(MediaType.TEXT_PLAIN).body("UP");
    }

    @GetMapping("/ready")
    public ResponseEntity<String> ready() {
        return ResponseEntity.ok().contentType(MediaType.TEXT_PLAIN).body("UP");
    }
}
