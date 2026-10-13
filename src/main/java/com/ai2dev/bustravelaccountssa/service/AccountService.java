package com.ai2dev.bustravelaccountssa.service;

import java.time.LocalDate;
import java.time.OffsetDateTime;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.UUID;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.dao.DataAccessException;
import org.springframework.http.HttpHeaders;
import org.springframework.stereotype.Service;

import com.ai2dev.bustravelaccountssa.client.AccountClient;
import com.ai2dev.bustravelaccountssa.exception.AccountNotFoundException;
import com.ai2dev.bustravelaccountssa.model.error.ErrorResponse;
import com.ai2dev.bustravelaccountssa.model.request.AccountRequest;
import com.ai2dev.bustravelaccountssa.model.response.AccountResponse;
import com.ai2dev.bustravelaccountssa.model.response.IdResponse;
import tools.jackson.databind.ObjectMapper;

@Service
public class AccountService {
    private static final Logger log = LoggerFactory.getLogger(AccountService.class);
    private final AccountClient client;
    private final ObjectMapper objectMapper;

    public AccountService(AccountClient client, ObjectMapper objectMapper) {
        this.client = client;
        this.objectMapper = objectMapper;
    }

    public Result list(String email, HttpHeaders headers) {
        try {
            List<AccountResponse> rows = client.findByEmail(email);
            if (rows.isEmpty()) {
                return Result.of(204, "[]");
            }
            return Result.of(200, objectMapper.writeValueAsString(rows));
        } catch (DataAccessException e) {
            log.error("SELECT public.sa_accounts failed", e);
            return dbError(e);
        } catch (Exception e) {
            log.error("Unexpected error", e);
            return unexpected();
        }
    }

    public Result getById(String id, HttpHeaders headers) {
        try {
            UUID uuid;
            try {
                uuid = UUID.fromString(id);
            } catch (IllegalArgumentException ex) {
                return Result.of(204, "[]");
            }
            AccountResponse row = client.findById(uuid);
            if (row == null) {
                return Result.of(204, "[]");
            }
            return Result.of(200, objectMapper.writeValueAsString(row));
        } catch (DataAccessException e) {
            log.error("SELECT public.sa_accounts failed", e);
            return dbError(e);
        } catch (Exception e) {
            log.error("Unexpected error", e);
            return unexpected();
        }
    }

    public Result create(AccountRequest request, HttpHeaders headers) {
        try {
            Map<String, Object> params = new HashMap<>();
            params.put("id", UUID.randomUUID());
            params.put("salutation", trim(request.salutation()));
            params.put("first_name", trim(request.firstName()));
            params.put("last_name", trim(request.lastName()));
            params.put("person_email", trim(request.personEmail()));
            params.put("person_birthdate", parseDate(request.personBirthdate()));
            params.put("phone", trim(request.phone()));
            params.put("mobile_phone", trim(request.mobilePhone()));
            params.put("mailing_street", trim(request.mailingStreet()));
            params.put("mailing_postal_code", trim(request.mailingPostalCode()));
            params.put("mailing_city", trim(request.mailingCity()));
            params.put("mailing_country", trim(request.mailingCountry()));
            params.put("account_status", trim(request.accountStatus()));
            params.put("account_source", trim(request.accountSource()));
            params.put("hotlisted", request.hotlisted() != null ? request.hotlisted() : false);
            IdResponse response = client.insert(params);
            return Result.of(200, objectMapper.writeValueAsString(response));
        } catch (DataAccessException e) {
            log.error("INSERT public.sa_accounts failed", e);
            return dbError(e);
        } catch (Exception e) {
            log.error("Unexpected error", e);
            return unexpected();
        }
    }

    public Result update(String id, AccountRequest request, HttpHeaders headers) {
        try {
            Map<String, Object> params = new HashMap<>();
            params.put("id", UUID.fromString(id));
            if (request.salutation() != null) params.put("salutation", trim(request.salutation()));
            if (request.firstName() != null) params.put("first_name", trim(request.firstName()));
            if (request.lastName() != null) params.put("last_name", trim(request.lastName()));
            if (request.personEmail() != null) params.put("person_email", trim(request.personEmail()));
            if (request.personBirthdate() != null) params.put("person_birthdate", parseDate(request.personBirthdate()));
            if (request.phone() != null) params.put("phone", trim(request.phone()));
            if (request.mobilePhone() != null) params.put("mobile_phone", trim(request.mobilePhone()));
            if (request.mailingStreet() != null) params.put("mailing_street", trim(request.mailingStreet()));
            if (request.mailingPostalCode() != null) params.put("mailing_postal_code", trim(request.mailingPostalCode()));
            if (request.mailingCity() != null) params.put("mailing_city", trim(request.mailingCity()));
            if (request.mailingCountry() != null) params.put("mailing_country", trim(request.mailingCountry()));
            if (request.accountStatus() != null) params.put("account_status", trim(request.accountStatus()));
            if (request.accountSource() != null) params.put("account_source", trim(request.accountSource()));
            if (request.hotlisted() != null) params.put("hotlisted", request.hotlisted());
            IdResponse response = client.update(params);
            return Result.of(200, objectMapper.writeValueAsString(response));
        } catch (AccountNotFoundException e) {
            log.warn("UPDATE public.sa_accounts matched no row");
            return Result.of(400, objectMapper.writeValueAsString(new ErrorResponse(new ErrorResponse.ErrorBody(400, OffsetDateTime.now(), "Account not found", "Account not found"))));
        } catch (DataAccessException e) {
            log.error("UPDATE public.sa_accounts failed", e);
            return dbError(e);
        } catch (Exception e) {
            log.error("Unexpected error", e);
            return unexpected();
        }
    }

    private Result dbError(DataAccessException e) {
        Throwable cause = e.getMostSpecificCause();
        String sqlState = cause instanceof java.sql.SQLException sql ? sql.getSQLState() : null;
        String message = cause != null ? cause.getMessage() : "Database error";
        try {
            return Result.of(400, objectMapper.writeValueAsString(new ErrorResponse(new ErrorResponse.ErrorBody(400, OffsetDateTime.now(), (sqlState == null ? "UNKNOWN" : sqlState) + " ERROR", message))));
        } catch (Exception ex) {
            log.error("Unexpected error", ex);
            return unexpected();
        }
    }

    private Result unexpected() {
        try {
            return Result.of(500, objectMapper.writeValueAsString(new ErrorResponse(new ErrorResponse.ErrorBody(500, OffsetDateTime.now(), "MULE UNKNOWN ERROR", "An unexpected error occurred"))));
        } catch (Exception ex) {
            log.error("Unexpected error", ex);
            throw new IllegalStateException(ex);
        }
    }

    private static String trim(String value) {
        return value == null ? null : value.trim();
    }

    private static LocalDate parseDate(String value) {
        return value == null ? null : LocalDate.parse(value);
    }

    public record Result(int statusCode, String body) {
        public static Result of(int statusCode, String body) { return new Result(statusCode, body); }
    }
}
