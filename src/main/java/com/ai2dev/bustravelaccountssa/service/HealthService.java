package com.ai2dev.bustravelaccountssa.service;

import com.ai2dev.bustravelaccountssa.client.SalesforceClient;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Service;

@Service
public class HealthService {
    private final SalesforceClient salesforceClient;

    public HealthService(SalesforceClient salesforceClient) {
        this.salesforceClient = salesforceClient;
    }

    public ResponseEntity<String> alive() {
        return ResponseEntity.ok().contentType(MediaType.TEXT_PLAIN).body("UP");
    }

    public ResponseEntity<String> ready() {
        salesforceClient.checkReady();
        return ResponseEntity.ok().contentType(MediaType.TEXT_PLAIN).body("UP");
    }
}
