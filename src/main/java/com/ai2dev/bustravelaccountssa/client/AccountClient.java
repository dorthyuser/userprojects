package com.ai2dev.bustravelaccountssa.client;

import java.sql.ResultSet;
import java.sql.SQLException;
import java.time.LocalDate;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.UUID;

import javax.sql.DataSource;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.context.annotation.Lazy;
import org.springframework.dao.DataAccessException;
import org.springframework.jdbc.core.namedparam.NamedParameterJdbcTemplate;
import org.springframework.stereotype.Component;

import com.ai2dev.bustravelaccountssa.exception.AccountNotFoundException;
import com.ai2dev.bustravelaccountssa.model.request.AccountCreateRequest;
import com.ai2dev.bustravelaccountssa.model.request.AccountUpdateRequest;
import com.ai2dev.bustravelaccountssa.model.response.AccountResponse;
import com.ai2dev.bustravelaccountssa.model.response.IdResponse;

@Component
public class AccountClient {
    private static final Logger log = LoggerFactory.getLogger(AccountClient.class);
    private final NamedParameterJdbcTemplate jdbc;

    public AccountClient(@Lazy DataSource dataSource) {
        this.jdbc = new NamedParameterJdbcTemplate(dataSource);
    }

    public List<AccountResponse> findByEmail(String email) {
        try {
            List<AccountResponse> results = jdbc.query(
                    "SELECT id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted FROM public.sa_accounts WHERE lower(person_email) = lower(:email)",
                    Map.of("email", email),
                    (rs, rowNum) -> mapAccount(rs)
            );
            log.info("PostgreSQL SELECT public.sa_accounts rows={}", results.size());
            return results;
        } catch (DataAccessException ex) {
            log.error("PostgreSQL SELECT public.sa_accounts failed", ex);
            throw ex;
        }
    }

    public AccountResponse findById(UUID id) {
        try {
            List<AccountResponse> results = jdbc.query(
                    "SELECT id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted FROM public.sa_accounts WHERE id = :id",
                    Map.of("id", id),
                    (rs, rowNum) -> mapAccount(rs)
            );
            log.info("PostgreSQL SELECT public.sa_accounts rows={}", results.size());
            return results.isEmpty() ? null : results.get(0);
        } catch (DataAccessException ex) {
            log.error("PostgreSQL SELECT public.sa_accounts failed", ex);
            throw ex;
        }
    }

    public IdResponse insert(AccountCreateRequest request, UUID id) {
        try {
            Map<String, Object> params = new HashMap<>();
            params.put("id", id);
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
            params.put("hotlisted", request.hotlisted() != null ? request.hotlisted() : Boolean.FALSE);
            jdbc.update(
                    "INSERT INTO public.sa_accounts (id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted) VALUES (:id, :salutation, :first_name, :last_name, :person_email, :person_birthdate, :phone, :mobile_phone, :mailing_street, :mailing_postal_code, :mailing_city, :mailing_country, :account_status, :account_source, :hotlisted)",
                    params
            );
            log.info("PostgreSQL INSERT public.sa_accounts rows=1");
            return new IdResponse(id.toString());
        } catch (DataAccessException ex) {
            log.error("PostgreSQL INSERT public.sa_accounts failed", ex);
            throw ex;
        }
    }

    public IdResponse update(UUID id, AccountUpdateRequest request) {
        try {
            List<String> sets = new ArrayList<>();
            Map<String, Object> params = new HashMap<>();
            params.put("id", id);
            sets.add("salutation = :salutation");
            params.put("salutation", trim(request.salutation()));
            sets.add("first_name = :first_name");
            params.put("first_name", trim(request.firstName()));
            sets.add("last_name = :last_name");
            params.put("last_name", trim(request.lastName()));
            sets.add("person_email = :person_email");
            params.put("person_email", trim(request.personEmail()));
            sets.add("person_birthdate = :person_birthdate");
            params.put("person_birthdate", parseDate(request.personBirthdate()));
            sets.add("phone = :phone");
            params.put("phone", trim(request.phone()));
            sets.add("mobile_phone = :mobile_phone");
            params.put("mobile_phone", trim(request.mobilePhone()));
            sets.add("mailing_street = :mailing_street");
            params.put("mailing_street", trim(request.mailingStreet()));
            sets.add("mailing_postal_code = :mailing_postal_code");
            params.put("mailing_postal_code", trim(request.mailingPostalCode()));
            sets.add("mailing_city = :mailing_city");
            params.put("mailing_city", trim(request.mailingCity()));
            sets.add("mailing_country = :mailing_country");
            params.put("mailing_country", trim(request.mailingCountry()));
            sets.add("account_status = :account_status");
            params.put("account_status", trim(request.accountStatus()));
            sets.add("account_source = :account_source");
            params.put("account_source", trim(request.accountSource()));
            sets.add("updated_at = now()");
            int updated = jdbc.update("UPDATE public.sa_accounts SET " + String.join(", ", sets) + " WHERE id = :id", params);
            log.info("PostgreSQL UPDATE public.sa_accounts rows={}", updated);
            if (updated == 0) {
                throw new AccountNotFoundException();
            }
            return new IdResponse(id.toString());
        } catch (DataAccessException ex) {
            log.error("PostgreSQL UPDATE public.sa_accounts failed", ex);
            throw ex;
        }
    }

    private AccountResponse mapAccount(ResultSet rs) throws SQLException {
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
                defaultEmpty(trim(rs.getString("account_source"))),
                rs.getObject("hotlisted", Boolean.class)
        );
    }

    private String trim(String value) {
        return value == null ? null : value.trim();
    }

    private String defaultEmpty(String value) {
        return value == null ? "" : value;
    }

    private LocalDate parseDate(String value) {
        return value == null ? null : LocalDate.parse(value.trim());
    }
}
