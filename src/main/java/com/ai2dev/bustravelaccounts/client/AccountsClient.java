package com.ai2dev.bustravelaccounts.client;

import com.ai2dev.bustravelaccounts.model.request.AccountRequest;
import com.ai2dev.bustravelaccounts.model.response.AccountResponse;
import com.ai2dev.bustravelaccounts.model.response.IdResponse;
import java.util.List;
import java.util.Optional;

public interface AccountsClient {
    List<AccountResponse> findByEmail(String email);
    Optional<AccountResponse> findById(String id);
    IdResponse create(AccountRequest request);
    IdResponse update(String id, AccountRequest request);
}
