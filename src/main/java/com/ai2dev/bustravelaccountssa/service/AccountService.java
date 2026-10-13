package com.ai2dev.bustravelaccountssa.service;

import java.time.OffsetDateTime;
import java.util.List;
import java.util.UUID;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;

import com.ai2dev.bustravelaccountssa.client.AccountClient;
import com.ai2dev.bustravelaccountssa.exception.AccountNotFoundException;
import com.ai2dev.bustravelaccountssa.model.error.ErrorResponse;
import com.ai2dev.bustravelaccountssa.model.request.AccountCreateRequest;
import com.ai2dev.bustravelaccountssa.model.request.AccountUpdateRequest;
import com.ai2dev.bustravelaccountssa.model.response.AccountResponse;
import com.ai2dev.bustravelaccountssa.model.response.IdResponse;
import com.ai2dev.bustravelaccountssa.util.UuidUtil;

@Service
public class AccountService {
    private static final Logger log = LoggerFactory.getLogger(AccountService.class);
    private final AccountClient client;

    public AccountService(AccountClient client) {
        this.client = client;
    }

    public List<AccountResponse> getAccounts(String email) {
        return client.findByEmail(email);
    }

    public AccountResponse getAccountById(String id) {
        if (!UuidUtil.isValidUuid(id)) {
            return null;
        }
        return client.findById(UUID.fromString(id));
    }

    public IdResponse createAccount(AccountCreateRequest request) {
        return client.insert(request, UUID.randomUUID());
    }

    public IdResponse updateAccount(String id, AccountUpdateRequest request) {
        if (!UuidUtil.isValidUuid(id)) {
            throw new AccountNotFoundException();
        }
        return client.update(UUID.fromString(id), request);
    }

    public ErrorResponse badRequest(String description) {
        return new ErrorResponse(new ErrorResponse.ErrorBody(400, OffsetDateTime.now(), "BAD REQUEST", description));
    }

    public ErrorResponse notFound(String description) {
        return new ErrorResponse(new ErrorResponse.ErrorBody(404, OffsetDateTime.now(), "RESOURCE NOT FOUND", description));
    }

    public ErrorResponse methodNotAllowed(String description) {
        return new ErrorResponse(new ErrorResponse.ErrorBody(405, OffsetDateTime.now(), "METHOD NOT ALLOWED", description));
    }

    public ErrorResponse unsupportedMediaType(String description) {
        return new ErrorResponse(new ErrorResponse.ErrorBody(415, OffsetDateTime.now(), "UNSUPPORTED MEDIA TYPE", description));
    }

    public ErrorResponse accountNotFound() {
        return new ErrorResponse(new ErrorResponse.ErrorBody(400, OffsetDateTime.now(), "Account not found", "Account not found"));
    }

    public ErrorResponse unexpected() {
        return new ErrorResponse(new ErrorResponse.ErrorBody(500, OffsetDateTime.now(), "MULE UNKNOWN ERROR", "An unexpected error occurred"));
    }

    public ErrorResponse databaseError(String sqlState, String message) {
        String errorMessage = (sqlState == null || sqlState.isBlank() ? "UNKNOWN" : sqlState) + " ERROR";
        return new ErrorResponse(new ErrorResponse.ErrorBody(400, OffsetDateTime.now(), errorMessage, message));
    }
}
