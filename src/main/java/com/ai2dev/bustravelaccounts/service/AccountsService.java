package com.ai2dev.bustravelaccounts.service;

import com.ai2dev.bustravelaccounts.client.AccountsRepository;
import com.ai2dev.bustravelaccounts.exception.AccountNotFoundException;
import com.ai2dev.bustravelaccounts.exception.BadRequestException;
import com.ai2dev.bustravelaccounts.exception.DuplicateAccountException;
import com.ai2dev.bustravelaccounts.exception.HotlistedAccountException;
import com.ai2dev.bustravelaccounts.model.request.AccountRequest;
import com.ai2dev.bustravelaccounts.model.response.AccountResponse;
import com.ai2dev.bustravelaccounts.model.response.IdResponse;
import java.util.List;
import org.springframework.stereotype.Service;

@Service
public class AccountsService {
    private final AccountsRepository repository;

    public AccountsService(AccountsRepository repository) {
        this.repository = repository;
    }

    public List<AccountResponse> getAccounts(String email) {
        return repository.findByEmail(email);
    }

    public AccountResponse getAccountById(String id) {
        return repository.findById(id).orElseThrow(() -> new AccountNotFoundException("Account not found"));
    }

    public IdResponse createAccount(AccountRequest request) {
        if (request == null) {
            throw new BadRequestException("check payload existence or method/payload combination requirement");
        }
        if (request.personEmail() != null && !repository.findByEmail(request.personEmail()).isEmpty()) {
            throw new DuplicateAccountException("An account with this email already exists");
        }
        return repository.create(request);
    }

    public IdResponse updateAccount(String id, AccountRequest request) {
        if (request == null) {
            throw new BadRequestException("check payload existence or method/payload combination requirement");
        }
        AccountResponse existing = repository.findById(id).orElseThrow(() -> new AccountNotFoundException("Account not found"));
        if (Boolean.TRUE.equals(existing.hotlisted())) {
            throw new HotlistedAccountException("Account is hotlisted and cannot be changed");
        }
        return repository.update(id, request);
    }
}
