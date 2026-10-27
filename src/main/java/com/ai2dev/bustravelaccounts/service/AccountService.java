package com.ai2dev.bustravelaccounts.service;

import java.time.LocalDate;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.dao.DataAccessException;
import org.springframework.http.HttpHeaders;
import org.springframework.stereotype.Service;
import com.ai2dev.bustravelaccounts.dto.request.AccountRequest;
import com.ai2dev.bustravelaccounts.dto.response.AccountResponse;
import com.ai2dev.bustravelaccounts.dto.response.IdResponse;
import com.ai2dev.bustravelaccounts.exception.BadRequestException;
import com.ai2dev.bustravelaccounts.exception.ConflictException;
import com.ai2dev.bustravelaccounts.exception.NotFoundException;
import com.ai2dev.bustravelaccounts.exception.UnprocessableEntityException;
import com.ai2dev.bustravelaccounts.repository.AccountRepository;
import com.ai2dev.bustravelaccounts.util.RequestBodyUtil;
import tools.jackson.databind.JsonNode;
import tools.jackson.databind.json.JsonMapper;

@Service
public class AccountService {
    private static final Logger log = LoggerFactory.getLogger(AccountService.class);
    private final AccountRepository repository;
    private final JsonMapper jsonMapper;

    public AccountService(AccountRepository repository, JsonMapper jsonMapper) {
        this.repository = repository;
        this.jsonMapper = jsonMapper;
    }

    public List<AccountResponse> getAccounts(HttpHeaders headers, String email) {
        return repository.findByEmail(email == null ? null : email.trim());
    }

    public AccountResponse getAccount(String id) {
        return repository.findById(id).orElse(null);
    }

    public IdResponse create(String body) {
        JsonNode node = RequestBodyUtil.parse(jsonMapper, body);
        AccountRequest request = toRequest(node);
        List<AccountResponse> existing = repository.findByEmail(request.personEmail() == null ? null : request.personEmail().trim());
        if (!existing.isEmpty()) {
            throw new ConflictException("An account with this email already exists");
        }
        Map<String, Object> values = new HashMap<>();
        values.put("id", java.util.UUID.randomUUID());
        values.put("salutation", trim(request.salutation()));
        values.put("first_name", trim(request.firstName()));
        values.put("last_name", trim(request.lastName()));
        values.put("person_email", trim(request.personEmail()));
        values.put("person_birthdate", request.personBirthdate() == null ? null : LocalDate.parse(request.personBirthdate()));
        values.put("phone", trim(request.phone()));
        values.put("mobile_phone", trim(request.mobilePhone()));
        values.put("mailing_street", trim(request.mailingStreet()));
        values.put("mailing_postal_code", trim(request.mailingPostalCode()));
        values.put("mailing_city", trim(request.mailingCity()));
        values.put("mailing_country", trim(request.mailingCountry()));
        values.put("account_status", trim(request.accountStatus()));
        values.put("account_source", trim(request.accountSource()));
        try {
            return new IdResponse(repository.insert(values));
        } catch (DataAccessException e) {
            throw new BadRequestException(sqlState(e) + " ERROR");
        }
    }

    public IdResponse update(String id, String body) {
        JsonNode node = RequestBodyUtil.parse(jsonMapper, body);
        AccountRequest request = toRequest(node);
        AccountResponse existing = repository.findById(id).orElse(null);
        if (existing == null) {
            throw new NotFoundException("Account not found");
        }
        if (Boolean.TRUE.equals(existing.hotlisted())) {
            throw new UnprocessableEntityException("Account is hotlisted and cannot be changed");
        }
        Map<String, Object> values = new HashMap<>();
        values.put("id", java.util.UUID.fromString(id));
        List<String> sets = new ArrayList<>();
        add(sets, values, "salutation", trim(request.salutation()));
        add(sets, values, "first_name", trim(request.firstName()));
        add(sets, values, "last_name", trim(request.lastName()));
        add(sets, values, "person_email", trim(request.personEmail()));
        add(sets, values, "person_birthdate", request.personBirthdate() == null ? null : LocalDate.parse(request.personBirthdate()));
        add(sets, values, "phone", trim(request.phone()));
        add(sets, values, "mobile_phone", trim(request.mobilePhone()));
        add(sets, values, "mailing_street", trim(request.mailingStreet()));
        add(sets, values, "mailing_postal_code", trim(request.mailingPostalCode()));
        add(sets, values, "mailing_city", trim(request.mailingCity()));
        add(sets, values, "mailing_country", trim(request.mailingCountry()));
        add(sets, values, "account_status", trim(request.accountStatus()));
        add(sets, values, "account_source", trim(request.accountSource()));
        try {
            return new IdResponse(repository.update(id, values, sets));
        } catch (DataAccessException e) {
            throw new BadRequestException(sqlState(e) + " ERROR");
        }
    }

    private AccountRequest toRequest(JsonNode node) {
        return new AccountRequest(
                text(node, "salutation"),
                text(node, "firstName"),
                text(node, "lastName"),
                text(node, "personEmail"),
                text(node, "personBirthdate"),
                text(node, "phone"),
                text(node, "mobilePhone"),
                text(node, "mailingStreet"),
                text(node, "mailingPostalCode"),
                text(node, "mailingCity"),
                text(node, "mailingCountry"),
                text(node, "accountStatus"),
                text(node, "accountSource")
        );
    }

    private String text(JsonNode node, String field) {
        JsonNode value = node == null ? null : node.get(field);
        return value == null || value.isNull() ? null : value.asString();
    }

    private void add(List<String> sets, Map<String, Object> values, String column, Object value) {
        if (value != null) {
            sets.add(column + " = :" + column);
            values.put(column, value);
        }
    }

    private String trim(String value) {
        return value == null ? null : value.trim();
    }

    private String sqlState(DataAccessException e) {
        Throwable cause = e.getMostSpecificCause();
        if (cause instanceof java.sql.SQLException sql) return sql.getSQLState();
        return "MULE UNKNOWN";
    }
}
