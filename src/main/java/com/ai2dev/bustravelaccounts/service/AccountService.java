package com.ai2dev.bustravelaccounts.service;

import com.ai2dev.bustravelaccounts.client.AccountRepository;
import com.ai2dev.bustravelaccounts.exception.AccountHotlistedException;
import com.ai2dev.bustravelaccounts.exception.AccountNotFoundException;
import com.ai2dev.bustravelaccounts.exception.BusinessRuleException;
import com.ai2dev.bustravelaccounts.model.request.AccountRequest;
import com.ai2dev.bustravelaccounts.model.response.AccountResponse;
import com.ai2dev.bustravelaccounts.model.response.IdResponse;
import java.util.List;
import org.springframework.stereotype.Service;

@Service
public class AccountService {
    private final AccountRepository repository;

    public AccountService(AccountRepository repository) {
        this.repository = repository;
    }

    public List<AccountResponse> getAccounts(String email) {
        return repository.findByEmail(email);
    }

    public AccountResponse getAccount(String id) {
        return repository.findById(id);
    }

    public IdResponse create(AccountRequest request) {
        List<AccountResponse> existing = repository.findByEmail(request.personEmail());
        if (existing != null && !existing.isEmpty()) {
            throw new BusinessRuleException(409, "An account with this email already exists");
        }
        return repository.create(request);
    }

    public IdResponse update(String id, AccountRequest request) {
        AccountResponse existing = repository.findById(id);
        if (existing == null) {
            throw new AccountNotFoundException("Account not found");
        }
        if (Boolean.TRUE.equals(existing.hotlisted())) {
            throw new AccountHotlistedException("Account is hotlisted and cannot be changed");
        }
        return repository.update(id, request);
    }

    public void validateCreate(AccountRequest request) {
        if (request == null) {
            throw new BusinessRuleException(400, "ERROR");
        }
    }
}
