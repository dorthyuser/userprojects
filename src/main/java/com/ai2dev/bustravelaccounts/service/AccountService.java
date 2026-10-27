package com.ai2dev.bustravelaccounts.service;

import com.ai2dev.bustravelaccounts.client.AccountRepository;
import com.ai2dev.bustravelaccounts.exception.ConflictException;
import com.ai2dev.bustravelaccounts.exception.HotlistedAccountException;
import com.ai2dev.bustravelaccounts.exception.NotFoundException;
import com.ai2dev.bustravelaccounts.exception.UnknownErrorException;
import com.ai2dev.bustravelaccounts.model.request.AccountRequest;
import com.ai2dev.bustravelaccounts.model.response.AccountResponse;
import com.ai2dev.bustravelaccounts.model.response.CreateUpdateResponse;
import java.util.List;
import org.springframework.stereotype.Service;

@Service
public class AccountService {
    private final AccountRepository repository;

    public AccountService(AccountRepository repository) {
        this.repository = repository;
    }

    public List<AccountResponse> getAccounts(String email) {
        return repository.getAccounts(email);
    }

    public AccountResponse getAccount(String id) {
        return repository.getAccount(id);
    }

    public CreateUpdateResponse create(AccountRequest request) {
        if (request == null) {
            throw new UnknownErrorException("MULE UNKNOWN ERROR");
        }
        if (request.personEmail() != null) {
            List<AccountResponse> existing = repository.getAccounts(request.personEmail());
            if (!existing.isEmpty()) {
                throw new ConflictException("An account with this email already exists");
            }
        }
        return repository.create(request);
    }

    public CreateUpdateResponse update(String id, AccountRequest request) {
        if (request == null) {
            throw new UnknownErrorException("MULE UNKNOWN ERROR");
        }
        AccountResponse existing = repository.getAccount(id);
        if (existing == null) {
            throw new NotFoundException("Account not found");
        }
        if (Boolean.TRUE.equals(existing.hotlisted())) {
            throw new HotlistedAccountException("Account is hotlisted and cannot be changed");
        }
        return repository.update(id, request);
    }
}
