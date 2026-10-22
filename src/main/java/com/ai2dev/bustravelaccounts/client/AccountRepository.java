package com.ai2dev.bustravelaccounts.client;

import com.ai2dev.bustravelaccounts.model.request.AccountRequest;
import com.ai2dev.bustravelaccounts.model.response.AccountResponse;
import com.ai2dev.bustravelaccounts.model.response.IdResponse;
import java.util.List;

public interface AccountRepository {
    List<AccountResponse> findByEmail(String email);
    AccountResponse findById(String id);
    IdResponse create(AccountRequest request);
    IdResponse update(String id, AccountRequest request);
}
