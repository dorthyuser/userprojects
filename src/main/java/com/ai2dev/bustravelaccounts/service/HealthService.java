package com.ai2dev.bustravelaccounts.service;

import com.ai2dev.bustravelaccounts.repository.AccountRepository;
import org.springframework.stereotype.Service;

@Service
public class HealthService {
    private final AccountRepository repository;

    public HealthService(AccountRepository repository) {
        this.repository = repository;
    }

    public String ready() {
        repository.findByEmail("");
        return "UP";
    }
}
