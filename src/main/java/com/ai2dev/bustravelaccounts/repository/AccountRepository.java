package com.ai2dev.bustravelaccounts.repository;

import java.time.LocalDate;
import java.util.List;
import java.util.Map;
import java.util.Optional;
import javax.sql.DataSource;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.context.annotation.Lazy;
import org.springframework.dao.DataAccessException;
import org.springframework.jdbc.core.simple.JdbcClient;
import org.springframework.stereotype.Repository;
import com.ai2dev.bustravelaccounts.dto.response.AccountResponse;

@Repository
public class AccountRepository {
    private static final Logger log = LoggerFactory.getLogger(AccountRepository.class);
    private final JdbcClient jdbc;

    public AccountRepository(@Lazy DataSource dataSource) {
        this.jdbc = JdbcClient.create(dataSource);
    }

    public List<AccountResponse> findByEmail(String email) {
        try {
            List<AccountResponse> rows = jdbc.sql("""
                    SELECT id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone,
                           mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status,
                           account_source, hotlisted
                    FROM public.sa_accounts
                    WHERE lower(person_email) = lower(:email)
                    ORDER BY id
                    """)
                    .param("email", email)
                    .query((rs, rowNum) -> new AccountResponse(
                            rs.getObject("id", java.util.UUID.class).toString(),
                            rs.getString("salutation"),
                            rs.getString("first_name"),
                            rs.getString("last_name"),
                            rs.getObject("person_birthdate", LocalDate.class) == null ? null : rs.getObject("person_birthdate", LocalDate.class).toString(),
                            rs.getString("phone"),
                            rs.getString("mobile_phone"),
                            rs.getString("person_email"),
                            rs.getString("mailing_street"),
                            rs.getString("mailing_postal_code"),
                            rs.getString("mailing_city"),
                            rs.getString("mailing_country"),
                            rs.getString("account_status"),
                            rs.getString("account_source"),
                            rs.getObject("hotlisted", Boolean.class)
                    )).list();
            log.info("DB GET /accounts outcome rows={}", rows.size());
            return rows;
        } catch (DataAccessException e) {
            log.error("DB GET /accounts failed", e);
            throw e;
        }
    }

    public Optional<AccountResponse> findById(String id) {
        try {
            List<AccountResponse> rows = jdbc.sql("""
                    SELECT id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone,
                           mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status,
                           account_source, hotlisted
                    FROM public.sa_accounts
                    WHERE id = :id
                    """)
                    .param("id", java.util.UUID.fromString(id))
                    .query((rs, rowNum) -> new AccountResponse(
                            rs.getObject("id", java.util.UUID.class).toString(),
                            rs.getString("salutation"),
                            rs.getString("first_name"),
                            rs.getString("last_name"),
                            rs.getObject("person_birthdate", LocalDate.class) == null ? null : rs.getObject("person_birthdate", LocalDate.class).toString(),
                            rs.getString("phone"),
                            rs.getString("mobile_phone"),
                            rs.getString("person_email"),
                            rs.getString("mailing_street"),
                            rs.getString("mailing_postal_code"),
                            rs.getString("mailing_city"),
                            rs.getString("mailing_country"),
                            rs.getString("account_status"),
                            rs.getString("account_source"),
                            rs.getObject("hotlisted", Boolean.class)
                    )).list();
            log.info("DB GET /accounts/{{id}} outcome rows={}", rows.size());
            return rows.stream().findFirst();
        } catch (DataAccessException e) {
            log.error("DB GET /accounts/{id} failed", e);
            throw e;
        }
    }

    public String insert(Map<String, Object> values) {
        try {
            String sql = """
                    INSERT INTO public.sa_accounts (id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source)
                    VALUES (:id, :salutation, :first_name, :last_name, :person_email, :person_birthdate, :phone, :mobile_phone, :mailing_street, :mailing_postal_code, :mailing_city, :mailing_country, :account_status, :account_source)
                    """;
            jdbc.sql(sql).params(values).update();
            log.info("DB INSERT public.sa_accounts outcome rows=1");
            return values.get("id").toString();
        } catch (DataAccessException e) {
            log.error("DB INSERT public.sa_accounts failed", e);
            throw e;
        }
    }

    public String update(String id, Map<String, Object> values, List<String> sets) {
        try {
            String sql = "UPDATE public.sa_accounts SET " + String.join(", ", sets) + ", updated_at = now() WHERE id = :id";
            jdbc.sql(sql).params(values).update();
            log.info("DB UPDATE public.sa_accounts outcome rows=1");
            return id;
        } catch (DataAccessException e) {
            log.error("DB UPDATE public.sa_accounts failed", e);
            throw e;
        }
    }
}
