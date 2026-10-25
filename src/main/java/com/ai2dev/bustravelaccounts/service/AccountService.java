package com.ai2dev.bustravelaccounts.service;

import java.util.HashMap;
import java.util.List;
import java.util.Map;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.HttpHeaders;
import org.springframework.stereotype.Service;
import com.ai2dev.bustravelaccounts.exception.BadRequestException;
import com.ai2dev.bustravelaccounts.exception.ConflictException;
import com.ai2dev.bustravelaccounts.exception.DatabaseOperationException;
import com.ai2dev.bustravelaccounts.exception.MuleUnknownException;
import com.ai2dev.bustravelaccounts.exception.NotFoundException;
import com.ai2dev.bustravelaccounts.exception.UnprocessableEntityException;
import com.ai2dev.bustravelaccounts.model.request.AccountRequest;
import com.ai2dev.bustravelaccounts.model.response.AccountResponse;
import com.ai2dev.bustravelaccounts.model.response.IdResponse;
import com.ai2dev.bustravelaccounts.repository.AccountRepository;
import com.ai2dev.bustravelaccounts.util.CorrelationIdUtil;

@Service
public class AccountService {
    private static final Logger log = LoggerFactory.getLogger(AccountService.class);
    private final AccountRepository repository;

    public AccountService(AccountRepository repository) {
        this.repository = repository;
    }

    public List<AccountResponse> getAccounts(HttpHeaders headers, String email) {
        log.info("Service get accounts correlationId={}", CorrelationIdUtil.correlationId(headers));
        return repository.findByEmail(email);
    }

    public AccountResponse getAccountById(HttpHeaders headers, String id) {
        log.info("Service get account by id correlationId={}", CorrelationIdUtil.correlationId(headers));
        return repository.findById(id);
    }

    public IdResponse createAccount(HttpHeaders headers, AccountRequest request) {
        if (request == null) throw new MuleUnknownException("check payload existence or method/payload combination requirement");
        if (request.personEmail() == null || request.personEmail().isBlank()) throw new BadRequestException("BAD REQUEST");
        List<AccountResponse> existing = repository.findByEmail(request.personEmail().trim());
        if (!existing.isEmpty()) {
            throw new ConflictException("An account with this email already exists");
        }
        try {
            Map<String, Object> values = toInsertValues(request);
            return new IdResponse(repository.create(values));
        } catch (RuntimeException e) {
            if (e instanceof org.springframework.dao.DataAccessException dae) {
                String sqlState = dae.getMostSpecificCause() instanceof java.sql.SQLException sql ? sql.getSQLState() : null;
                throw new DatabaseOperationException(sqlState, sqlState + " ERROR");
            }
            throw e;
        }
    }

    public IdResponse updateAccount(HttpHeaders headers, String id, AccountRequest request) {
        if (request == null) throw new MuleUnknownException("check payload existence or method/payload combination requirement");
        AccountResponse existing = repository.findById(id);
        if (existing == null) {
            throw new NotFoundException("Account not found");
        }
        if (Boolean.TRUE.equals(existing.hotlisted())) {
            throw new UnprocessableEntityException("Account is hotlisted and cannot be changed");
        }
        try {
            Map<String, Object> values = toUpdateValues(request);
            return new IdResponse(repository.update(id, values));
        } catch (RuntimeException e) {
            if (e instanceof org.springframework.dao.DataAccessException dae) {
                String sqlState = dae.getMostSpecificCause() instanceof java.sql.SQLException sql ? sql.getSQLState() : null;
                throw new DatabaseOperationException(sqlState, sqlState + " ERROR");
            }
            throw e;
        }
    }

    private Map<String, Object> toInsertValues(AccountRequest request) {
        Map<String, Object> values = new HashMap<>();
        values.put("salutation", trim(request.salutation()));
        values.put("first_name", trim(request.firstName()));
        values.put("last_name", trim(request.lastName()));
        values.put("person_email", trim(request.personEmail()));
        values.put("person_birthdate", request.personBirthdate() == null ? null : request.personBirthdate().trim());
        values.put("phone", trim(request.phone()));
        values.put("mobile_phone", trim(request.mobilePhone()));
        values.put("mailing_street", trim(request.mailingStreet()));
        values.put("mailing_postal_code", trim(request.mailingPostalCode()));
        values.put("mailing_city", trim(request.mailingCity()));
        values.put("mailing_country", trim(request.mailingCountry()));
        values.put("account_status", trim(request.accountStatus()));
        values.put("account_source", trim(request.accountSource()));
        return values;
    }

    private Map<String, Object> toUpdateValues(AccountRequest request) {
        Map<String, Object> values = new HashMap<>();
        if (request.salutation() != null) values.put("salutation", trim(request.salutation()));
        if (request.firstName() != null) values.put("first_name", trim(request.firstName()));
        if (request.lastName() != null) values.put("last_name", trim(request.lastName()));
        if (request.personEmail() != null) values.put("person_email", trim(request.personEmail()));
        if (request.personBirthdate() != null) values.put("person_birthdate", request.personBirthdate().trim());
        if (request.phone() != null) values.put("phone", trim(request.phone()));
        if (request.mobilePhone() != null) values.put("mobile_phone", trim(request.mobilePhone()));
        if (request.mailingStreet() != null) values.put("mailing_street", trim(request.mailingStreet()));
        if (request.mailingPostalCode() != null) values.put("mailing_postal_code", trim(request.mailingPostalCode()));
        if (request.mailingCity() != null) values.put("mailing_city", trim(request.mailingCity()));
        if (request.mailingCountry() != null) values.put("mailing_country", trim(request.mailingCountry()));
        if (request.accountStatus() != null) values.put("account_status", trim(request.accountStatus()));
        if (request.accountSource() != null) values.put("account_source", trim(request.accountSource()));
        return values;
    }

    private String trim(String value) { return value == null ? null : value.trim(); }
}
