package com.ai2dev.bustravelaccounts.client;

import com.ai2dev.bustravelaccounts.exception.AccountNotFoundException;
import com.ai2dev.bustravelaccounts.model.request.AccountRequest;
import com.ai2dev.bustravelaccounts.model.response.AccountResponse;
import com.ai2dev.bustravelaccounts.model.response.IdResponse;
import java.sql.ResultSet;
import java.sql.SQLException;
import java.time.LocalDate;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.UUID;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.context.annotation.Lazy;
import org.springframework.dao.DataAccessException;
import org.springframework.jdbc.core.simple.JdbcClient;
import org.springframework.stereotype.Component;

@Component
public class PostgresAccountRepository implements AccountRepository {
    private static final Logger log = LoggerFactory.getLogger(PostgresAccountRepository.class);
    private final JdbcClient jdbc;

    public PostgresAccountRepository(@Lazy JdbcClient jdbc) {
        this.jdbc = jdbc;
    }

    @Override
    public List<AccountResponse> findByEmail(String email) {
        try {
            List<AccountResponse> rows = jdbc.sql("SELECT id, salutation, first_name, last_name, person_birthdate, phone, mobile_phone, person_email, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted FROM public.sa_accounts WHERE lower(person_email) = lower(:email)")
                    .param("email", email)
                    .query((rs, rowNum) -> mapRow(rs))
                    .list();
            log.info("SELECT public.sa_accounts by email returned {} rows", rows.size());
            return rows;
        } catch (DataAccessException e) {
            log.error("SELECT public.sa_accounts by email failed", e);
            throw e;
        }
    }

    @Override
    public AccountResponse findById(String id) {
        try {
            List<AccountResponse> rows = jdbc.sql("SELECT id, salutation, first_name, last_name, person_birthdate, phone, mobile_phone, person_email, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted FROM public.sa_accounts WHERE id = :id")
                    .param("id", UUID.fromString(id))
                    .query((rs, rowNum) -> mapRow(rs))
                    .list();
            log.info("SELECT public.sa_accounts by id returned {} rows", rows.size());
            if (rows.isEmpty()) {
                throw new AccountNotFoundException("Account not found");
            }
            return rows.get(0);
        } catch (IllegalArgumentException e) {
            log.error("Invalid account id", e);
            throw new AccountNotFoundException("Account not found");
        } catch (DataAccessException e) {
            log.error("SELECT public.sa_accounts by id failed", e);
            throw e;
        }
    }

    @Override
    public IdResponse create(AccountRequest request) {
        try {
            String id = UUID.randomUUID().toString();
            jdbc.sql("INSERT INTO public.sa_accounts (id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, created_at, updated_at) VALUES (:id, :salutation, :firstName, :lastName, :personEmail, :personBirthdate, :phone, :mobilePhone, :mailingStreet, :mailingPostalCode, :mailingCity, :mailingCountry, :accountStatus, :accountSource, now(), now())")
                    .param("id", UUID.fromString(id))
                    .param("salutation", request.salutation())
                    .param("firstName", request.firstName())
                    .param("lastName", request.lastName())
                    .param("personEmail", request.personEmail())
                    .param("personBirthdate", request.personBirthdate())
                    .param("phone", request.phone())
                    .param("mobilePhone", request.mobilePhone())
                    .param("mailingStreet", request.mailingStreet())
                    .param("mailingPostalCode", request.mailingPostalCode())
                    .param("mailingCity", request.mailingCity())
                    .param("mailingCountry", request.mailingCountry())
                    .param("accountStatus", request.accountStatus())
                    .param("accountSource", request.accountSource())
                    .update();
            log.info("INSERT public.sa_accounts succeeded");
            return new IdResponse(id);
        } catch (DataAccessException e) {
            log.error("INSERT public.sa_accounts failed", e);
            throw e;
        }
    }

    @Override
    public IdResponse update(String id, AccountRequest request) {
        try {
            findById(id);
            List<String> sets = new ArrayList<>();
            Map<String, Object> params = new HashMap<>();
            params.put("id", UUID.fromString(id));
            sets.add("salutation = :salutation"); params.put("salutation", request.salutation());
            sets.add("first_name = :firstName"); params.put("firstName", request.firstName());
            sets.add("last_name = :lastName"); params.put("lastName", request.lastName());
            sets.add("person_email = :personEmail"); params.put("personEmail", request.personEmail());
            sets.add("person_birthdate = :personBirthdate"); params.put("personBirthdate", request.personBirthdate());
            if (request.phone() != null) { sets.add("phone = :phone"); params.put("phone", request.phone()); }
            if (request.mobilePhone() != null) { sets.add("mobile_phone = :mobilePhone"); params.put("mobilePhone", request.mobilePhone()); }
            sets.add("mailing_street = :mailingStreet"); params.put("mailingStreet", request.mailingStreet());
            sets.add("mailing_postal_code = :mailingPostalCode"); params.put("mailingPostalCode", request.mailingPostalCode());
            sets.add("mailing_city = :mailingCity"); params.put("mailingCity", request.mailingCity());
            sets.add("mailing_country = :mailingCountry"); params.put("mailingCountry", request.mailingCountry());
            sets.add("account_status = :accountStatus"); params.put("accountStatus", request.accountStatus());
            sets.add("account_source = :accountSource"); params.put("accountSource", request.accountSource());
            sets.add("updated_at = now()");
            jdbc.sql("UPDATE public.sa_accounts SET " + String.join(", ", sets) + " WHERE id = :id")
                    .params(params)
                    .update();
            log.info("UPDATE public.sa_accounts succeeded");
            return new IdResponse(id);
        } catch (DataAccessException e) {
            log.error("UPDATE public.sa_accounts failed", e);
            throw e;
        }
    }

    private AccountResponse mapRow(ResultSet rs) throws SQLException {
        return new AccountResponse(
                rs.getString("id"),
                trim(rs.getString("salutation")),
                trim(rs.getString("first_name")),
                trim(rs.getString("last_name")),
                rs.getObject("person_birthdate", LocalDate.class),
                trim(rs.getString("phone")),
                trim(rs.getString("mobile_phone")),
                trim(rs.getString("person_email")),
                trim(rs.getString("mailing_street")),
                trim(rs.getString("mailing_postal_code")),
                trim(rs.getString("mailing_city")),
                trim(rs.getString("mailing_country")),
                trim(rs.getString("account_status")),
                trim(rs.getString("account_source")),
                rs.getBoolean("hotlisted")
        );
    }

    private String trim(String value) {
        return value == null ? null : value.trim();
    }
}
