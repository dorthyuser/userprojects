package com.ai2dev.bustravelaccounts.controller;

import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping
public class HealthController {
    @GetMapping(value = "/alive", produces = MediaType.TEXT_PLAIN_VALUE)
    public ResponseEntity<String> alive() {
        return ResponseEntity.ok("UP");
    }

    @GetMapping(value = "/ready", produces = MediaType.TEXT_PLAIN_VALUE)
    public ResponseEntity<String> ready() {
        return ResponseEntity.ok("UP");
    }
}
