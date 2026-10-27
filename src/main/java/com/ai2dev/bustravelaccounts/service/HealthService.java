package com.ai2dev.bustravelaccounts.service;

import org.springframework.stereotype.Service;

@Service
public class HealthService {
    public String alive() {
        return "UP";
    }

    public String ready() {
        return "UP";
    }
}
