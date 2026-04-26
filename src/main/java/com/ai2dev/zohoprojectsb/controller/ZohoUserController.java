package com.ai2dev.zohoprojectsb.controller;

import com.ai2dev.zohoprojectsb.model.UserRequest;
import com.ai2dev.zohoprojectsb.service.IZohoCrmService;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.validation.annotation.Validated;
import org.springframework.web.bind.annotation.*;

import jakarta.validation.Valid;

@RestController
@RequestMapping(path = "/users", produces = MediaType.APPLICATION_JSON_VALUE)
@Validated
public class ZohoUserController {
    private static final Logger logger = LoggerFactory.getLogger(ZohoUserController.class);

    private final IZohoCrmService service;

    public ZohoUserController(IZohoCrmService service) {
        this.service = service;
    }

    @GetMapping
    public ResponseEntity<String> getUsers() {
        logger.info("Controller getUsers called");
        try {
            ResponseEntity<String> resp = service.getUsers();
            return ResponseEntity.status(resp.getStatusCode()).contentType(MediaType.APPLICATION_JSON).body(resp.getBody());
        } catch (Exception ex) {
            logger.error("Controller getUsers error", ex);
            String safe = jsonEscape(ex.getMessage());
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR).body("{"error":"" + safe + ""}");
        }
    }

    @PostMapping(consumes = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<String> createUser(@Valid @RequestBody UserRequest req) {
        logger.info("Controller createUser called");
        try {
            ResponseEntity<String> resp = service.createUser(req);
            return ResponseEntity.status(resp.getStatusCode()).contentType(MediaType.APPLICATION_JSON).body(resp.getBody());
        } catch (Exception ex) {
            logger.error("Controller createUser error", ex);
            String safe = jsonEscape(ex.getMessage());
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR).body("{"error":"" + safe + ""}");
        }
    }

    @PutMapping(path = "/{id}", consumes = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<String> updateUser(@PathVariable("id") String id, @Valid @RequestBody UserRequest req) {
        logger.info("Controller updateUser called for id {}", id);
        try {
            ResponseEntity<String> resp = service.updateUser(id, req);
            return ResponseEntity.status(resp.getStatusCode()).contentType(MediaType.APPLICATION_JSON).body(resp.getBody());
        } catch (Exception ex) {
            logger.error("Controller updateUser error", ex);
            String safe = jsonEscape(ex.getMessage());
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR).body("{"error":"" + safe + ""}");
        }
    }

    private String jsonEscape(String s) {
        if (s == null) return "";
        return s.replace("\", "\\\").replace(""", "\\"");
    }
}
