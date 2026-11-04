package com.ai2dev.odata4accounts.service;

import com.ai2dev.odata4accounts.repository.AccountRepository;
import java.util.Map;
import org.springframework.stereotype.Service;

@Service
public class AccountService {
    private final AccountRepository repository;

    public AccountService(AccountRepository repository) {
        this.repository = repository;
    }

    public Map<String, Object> create(Map<String, Object> values) {
        return repository.create(values);
    }
}
