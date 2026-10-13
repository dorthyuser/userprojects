package com.ai2dev.bustravelaccountssa.controller;

import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

@RestController
public class HealthController {
    @RequestMapping(value = "/alive", produces = MediaType.TEXT_PLAIN_VALUE)
    public ResponseEntity<String> alive() {
        return ResponseEntity.ok("UP");
    }

    @RequestMapping(value = "/ready", produces = MediaType.TEXT_PLAIN_VALUE)
    public ResponseEntity<String> ready() {
        return ResponseEntity.ok("UP");
    }
}
