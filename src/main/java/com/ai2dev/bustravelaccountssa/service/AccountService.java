package com.ai2dev.bustravelaccountssa.service;

import java.time.LocalDate;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.UUID;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.dao.DataAccessException;
import org.springframework.stereotype.Service;

import com.ai2dev.bustravelaccountssa.client.AccountClient;
import com.ai2dev.bustravelaccountssa.exception.AccountNotFoundException;
import com.ai2dev.bustravelaccountssa.model.request.AccountCreateRequest;
import com.ai2dev.bustravelaccountssa.model.request.AccountUpdateRequest;
import com.ai2dev.bustravelaccountssa.model.response.AccountResponse;
import com.ai2dev.bustravelaccountssa.model.response.IdResponse;
import com.ai2dev.bustravelaccountssa.util.AccountMapper;

@Service
public class AccountService {
    private static final Logger log = LoggerFactory.getLogger(AccountService.class);
    private final AccountClient client;

    public AccountService(AccountClient client) {
        this.client = client;
    }

    public List<AccountResponse> getAccounts(String email) {
        List<Map<String, Object>> rows = client.findByEmail(email);
        List<AccountResponse> responses = new ArrayList<>();
        for (Map<String, Object> row : rows) {
            responses.add(AccountMapper.fromRow(row));
        }
        return responses;
    }

    public AccountResponse getAccount(String id) {
        List<Map<String, Object>> rows = client.findById(id);
        if (rows.isEmpty()) {
            throw new AccountNotFoundException();
        }
        return AccountMapper.fromRow(rows.get(0));
    }

    public IdResponse create(AccountCreateRequest request) {
        Map<String, Object> params = new HashMap<>();
        params.put("id", UUID.randomUUID());
        params.put("salutation", trim(request.salutation()));
        params.put("first_name", trim(request.firstName()));
        params.put("last_name", trim(request.lastName()));
        params.put("person_email", trim(request.personEmail()));
        params.put("person_birthdate", request.personBirthdate() == null ? null : LocalDate.parse(request.personBirthdate()));
        params.put("phone", trim(request.phone()));
        params.put("mobile_phone", trim(request.mobilePhone()));
        params.put("mailing_street", trim(request.mailingStreet()));
        params.put("mailing_postal_code", trim(request.mailingPostalCode()));
        params.put("mailing_city", trim(request.mailingCity()));
        params.put("mailing_country", trim(request.mailingCountry()));
        params.put("account_status", trim(request.accountStatus()));
        params.put("account_source", trim(request.accountSource()));
        params.put("hotlisted", request.hotlisted() != null && request.hotlisted());
        try {
            client.insert(params);
            return new IdResponse(params.get("id").toString());
        } catch (DataAccessException e) {
            log.error("INSERT public.sa_accounts failed", e);
            throw e;
        }
    }

    public IdResponse update(String id, AccountUpdateRequest request) {
        List<String> sets = new ArrayList<>();
        Map<String, Object> params = new HashMap<>(Map.of("id", UUID.fromString(id)));
        add(sets, params, "salutation", trim(request.salutation()));
        add(sets, params, "first_name", trim(request.firstName()));
        add(sets, params, "last_name", trim(request.lastName()));
        add(sets, params, "person_email", trim(request.personEmail()));
        if (request.personBirthdate() != null) {
            sets.add("person_birthdate = :person_birthdate");
            params.put("person_birthdate", LocalDate.parse(request.personBirthdate()));
        }
        add(sets, params, "phone", trim(request.phone()));
        add(sets, params, "mobile_phone", trim(request.mobilePhone()));
        add(sets, params, "mailing_street", trim(request.mailingStreet()));
        add(sets, params, "mailing_postal_code", trim(request.mailingPostalCode()));
        add(sets, params, "mailing_city", trim(request.mailingCity()));
        add(sets, params, "mailing_country", trim(request.mailingCountry()));
        add(sets, params, "account_status", trim(request.accountStatus()));
        add(sets, params, "account_source", trim(request.accountSource()));
        if (request.hotlisted() != null) {
            sets.add("hotlisted = :hotlisted");
            params.put("hotlisted", request.hotlisted());
        }
        sets.add("updated_at = now()");
        String sql = "UPDATE public.sa_accounts SET " + String.join(", ", sets) + " WHERE id = :id";
        try {
            int updated = client.update(params, sql);
            if (updated == 0) {
                throw new AccountNotFoundException();
            }
            return new IdResponse(id);
        } catch (AccountNotFoundException e) {
            throw e;
        } catch (DataAccessException e) {
            log.error("UPDATE public.sa_accounts failed", e);
            throw e;
        }
    }

    public boolean ready() {
        client.findByEmail("ready-check@example.com");
        return true;
    }

    private static void add(List<String> sets, Map<String, Object> params, String column, String value) {
        if (value != null) {
            sets.add(column + " = :" + column);
            params.put(column, value);
        }
    }

    private static String trim(String value) {
        return value == null ? null : value.trim();
    }
}
