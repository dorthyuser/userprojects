package com.ai2dev.bustravelaccountssa.client;

import java.time.LocalDate;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.UUID;

import javax.sql.DataSource;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.dao.DataAccessException;
import org.springframework.context.annotation.Lazy;
import org.springframework.jdbc.core.namedparam.NamedParameterJdbcTemplate;
import org.springframework.stereotype.Component;

import com.ai2dev.bustravelaccountssa.exception.AccountNotFoundException;
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
            List<AccountResponse> rows = jdbc.query("SELECT id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted FROM public.sa_accounts WHERE lower(person_email) = lower(:email)", Map.of("email", email), (rs, rowNum) -> new AccountResponse(
                    rs.getObject("id", UUID.class).toString(),
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
                    rs.getObject("hotlisted", Boolean.class)));
            log.info("PostgreSQL GET /accounts status {}", rows.isEmpty() ? 204 : 200);
            return rows;
        } catch (DataAccessException e) {
            log.error("PostgreSQL GET /accounts failed", e);
            throw e;
        }
    }

    public AccountResponse findById(UUID id) {
        try {
            List<AccountResponse> rows = jdbc.query("SELECT id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted FROM public.sa_accounts WHERE id = :id", Map.of("id", id), (rs, rowNum) -> new AccountResponse(
                    rs.getObject("id", UUID.class).toString(),
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
                    rs.getObject("hotlisted", Boolean.class)));
            log.info("PostgreSQL GET /accounts/{{id}} status {}", rows.isEmpty() ? 204 : 200);
            return rows.isEmpty() ? null : rows.get(0);
        } catch (DataAccessException e) {
            log.error("PostgreSQL GET /accounts/{id} failed", e);
            throw e;
        }
    }

    public IdResponse insert(Map<String, Object> params) {
        try {
            jdbc.update("INSERT INTO public.sa_accounts (id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted) VALUES (:id, :salutation, :first_name, :last_name, :person_email, :person_birthdate, :phone, :mobile_phone, :mailing_street, :mailing_postal_code, :mailing_city, :mailing_country, :account_status, :account_source, :hotlisted)", params);
            log.info("PostgreSQL INSERT public.sa_accounts status 200");
            return new IdResponse(params.get("id").toString());
        } catch (DataAccessException e) {
            log.error("PostgreSQL INSERT public.sa_accounts failed", e);
            throw e;
        }
    }

    public IdResponse update(Map<String, Object> params) {
        try {
            List<String> sets = new ArrayList<>();
            if (params.containsKey("salutation")) sets.add("salutation = :salutation");
            if (params.containsKey("first_name")) sets.add("first_name = :first_name");
            if (params.containsKey("last_name")) sets.add("last_name = :last_name");
            if (params.containsKey("person_email")) sets.add("person_email = :person_email");
            if (params.containsKey("person_birthdate")) sets.add("person_birthdate = :person_birthdate");
            if (params.containsKey("phone")) sets.add("phone = :phone");
            if (params.containsKey("mobile_phone")) sets.add("mobile_phone = :mobile_phone");
            if (params.containsKey("mailing_street")) sets.add("mailing_street = :mailing_street");
            if (params.containsKey("mailing_postal_code")) sets.add("mailing_postal_code = :mailing_postal_code");
            if (params.containsKey("mailing_city")) sets.add("mailing_city = :mailing_city");
            if (params.containsKey("mailing_country")) sets.add("mailing_country = :mailing_country");
            if (params.containsKey("account_status")) sets.add("account_status = :account_status");
            if (params.containsKey("account_source")) sets.add("account_source = :account_source");
            if (params.containsKey("hotlisted")) sets.add("hotlisted = :hotlisted");
            sets.add("updated_at = now()");
            int updated = jdbc.update("UPDATE public.sa_accounts SET " + String.join(", ", sets) + " WHERE id = :id", params);
            log.info("PostgreSQL UPDATE public.sa_accounts status {}", updated);
            if (updated == 0) {
                throw new AccountNotFoundException("Account not found");
            }
            return new IdResponse(params.get("id").toString());
        } catch (DataAccessException e) {
            log.error("PostgreSQL UPDATE public.sa_accounts failed", e);
            throw e;
        }
    }

    private static String trim(String value) {
        return value == null ? null : value.trim();
    }
}
