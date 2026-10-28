package com.ai2dev.bustravelaccounts.service;

import com.ai2dev.bustravelaccounts.dto.request.AccountRequest;
import com.ai2dev.bustravelaccounts.dto.response.AccountResponse;
import com.ai2dev.bustravelaccounts.exception.AccountAlreadyExistsException;
import com.ai2dev.bustravelaccounts.exception.AccountNotFoundException;
import com.ai2dev.bustravelaccounts.exception.UnprocessableEntityException;
import com.ai2dev.bustravelaccounts.repository.AccountRepository;
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
        AccountResponse account = repository.findById(id);
        if (account == null) {
            throw new AccountNotFoundException("Account not found");
        }
        return account;
    }

    public String create(AccountRequest request) {
        List<AccountResponse> existing = repository.findByEmail(request.personEmail());
        if (existing != null && !existing.isEmpty()) {
            throw new AccountAlreadyExistsException("An account with this email already exists");
        }
        return repository.insert(request);
    }

    public String update(String id, AccountRequest request) {
        AccountResponse existing = repository.findById(id);
        if (existing == null) {
            throw new AccountNotFoundException("Account not found");
        }
        if (Boolean.TRUE.equals(existing.hotlisted())) {
            throw new UnprocessableEntityException("Account is hotlisted and cannot be changed");
        }
        return repository.update(id, request);
    }
}
