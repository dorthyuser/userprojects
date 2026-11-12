package com.ai2dev.bustravelaccountsea.service;

import com.ai2dev.bustravelaccountsea.client.ErrorArchiveClient;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Service;

@Service
public class HealthService {
    private final ErrorArchiveClient errorArchiveClient;

    public HealthService(ErrorArchiveClient errorArchiveClient) {
        this.errorArchiveClient = errorArchiveClient;
    }

    public ResponseEntity<String> alive() {
        return ResponseEntity.ok("UP");
    }

    public ResponseEntity<String> ready() {
        errorArchiveClient.ping();
        return ResponseEntity.ok("UP");
    }
}
