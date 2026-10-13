package com.ai2dev.bustravelaccountssa.client;

import java.util.List;
import java.util.Map;
import java.util.UUID;

import javax.sql.DataSource;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.context.annotation.Lazy;
import org.springframework.dao.DataAccessException;
import org.springframework.jdbc.core.simple.JdbcClient;
import org.springframework.stereotype.Component;

@Component
public class AccountClient {
    private static final Logger log = LoggerFactory.getLogger(AccountClient.class);
    private final JdbcClient jdbcClient;

    public AccountClient(@Lazy DataSource dataSource) {
        this.jdbcClient = JdbcClient.create(dataSource);
    }

    public List<Map<String, Object>> findByEmail(String email) {
        try {
            List<Map<String, Object>> rows = jdbcClient.sql("SELECT id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted FROM public.sa_accounts WHERE lower(person_email) = lower(:email)")
                    .param("email", email)
                    .query()
                    .listOfRows();
            log.info("PostgreSQL SELECT public.sa_accounts by lower(person_email) rows {}", rows.size());
            return rows;
        } catch (DataAccessException e) {
            log.error("PostgreSQL SELECT public.sa_accounts by lower(person_email) failed", e);
            throw e;
        }
    }

    public List<Map<String, Object>> findById(String id) {
        try {
            UUID uuid;
            try {
                uuid = UUID.fromString(id);
            } catch (IllegalArgumentException e) {
                log.info("PostgreSQL SELECT public.sa_accounts by id rows 0");
                return List.of();
            }
            List<Map<String, Object>> rows = jdbcClient.sql("SELECT id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted FROM public.sa_accounts WHERE id = :id")
                    .param("id", uuid)
                    .query()
                    .listOfRows();
            log.info("PostgreSQL SELECT public.sa_accounts by id rows {}", rows.size());
            return rows;
        } catch (DataAccessException e) {
            log.error("PostgreSQL SELECT public.sa_accounts by id failed", e);
            throw e;
        }
    }

    public Map<String, Object> insert(Map<String, Object> params) {
        try {
            int updated = jdbcClient.sql("INSERT INTO public.sa_accounts (id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted) VALUES (:id, :salutation, :first_name, :last_name, :person_email, :person_birthdate, :phone, :mobile_phone, :mailing_street, :mailing_postal_code, :mailing_city, :mailing_country, :account_status, :account_source, :hotlisted)")
                    .params(params)
                    .update();
            log.info("PostgreSQL INSERT public.sa_accounts rows {}", updated);
            return params;
        } catch (DataAccessException e) {
            log.error("PostgreSQL INSERT public.sa_accounts failed", e);
            throw e;
        }
    }

    public int update(Map<String, Object> params, String sql) throws DataAccessException {
        try {
            int updated = jdbcClient.sql(sql).params(params).update();
            log.info("PostgreSQL UPDATE public.sa_accounts rows {}", updated);
            return updated;
        } catch (DataAccessException e) {
            log.error("PostgreSQL UPDATE public.sa_accounts failed", e);
            throw e;
        }
    }
}
