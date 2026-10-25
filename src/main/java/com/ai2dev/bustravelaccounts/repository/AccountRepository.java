package com.ai2dev.bustravelaccounts.repository;

import java.time.OffsetDateTime;
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
import org.springframework.jdbc.core.simple.JdbcClient;
import org.springframework.stereotype.Repository;
import com.ai2dev.bustravelaccounts.model.response.AccountResponse;

@Repository
public class AccountRepository {
    private static final Logger log = LoggerFactory.getLogger(AccountRepository.class);
    private final JdbcClient jdbc;

    public AccountRepository(@Lazy DataSource dataSource) {
        this.jdbc = JdbcClient.create(dataSource);
    }

    public List<AccountResponse> findByEmail(String email) {
        try {
            log.info("DB GET accounts by email");
            List<AccountResponse> result = jdbc.sql("""
                    select id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone,
                           mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted
                    from public.sa_accounts
                    where lower(person_email) = lower(:email)
                    order by id
                    """)
                    .param("email", email)
                    .query((rs, rowNum) -> new AccountResponse(
                            rs.getObject("id", UUID.class).toString(),
                            rs.getString("salutation"),
                            rs.getString("first_name"),
                            rs.getString("last_name"),
                            rs.getObject("person_birthdate") == null ? null : rs.getObject("person_birthdate").toString(),
                            rs.getString("phone"),
                            rs.getString("mobile_phone"),
                            rs.getString("person_email"),
                            rs.getString("mailing_street"),
                            rs.getString("mailing_postal_code"),
                            rs.getString("mailing_city"),
                            rs.getString("mailing_country"),
                            rs.getString("account_status"),
                            rs.getString("account_source"),
                            rs.getBoolean("hotlisted")
                    )).list();
            log.info("DB GET accounts by email outcome rows={}", result.size());
            return result;
        } catch (DataAccessException e) {
            log.error("DB GET accounts by email failed", e);
            throw e;
        }
    }

    public AccountResponse findById(String id) {
        try {
            log.info("DB GET account by id");
            return jdbc.sql("""
                    select id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone,
                           mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted
                    from public.sa_accounts
                    where id = :id
                    """)
                    .param("id", UUID.fromString(id))
                    .query((rs, rowNum) -> new AccountResponse(
                            rs.getObject("id", UUID.class).toString(),
                            rs.getString("salutation"),
                            rs.getString("first_name"),
                            rs.getString("last_name"),
                            rs.getObject("person_birthdate") == null ? null : rs.getObject("person_birthdate").toString(),
                            rs.getString("phone"),
                            rs.getString("mobile_phone"),
                            rs.getString("person_email"),
                            rs.getString("mailing_street"),
                            rs.getString("mailing_postal_code"),
                            rs.getString("mailing_city"),
                            rs.getString("mailing_country"),
                            rs.getString("account_status"),
                            rs.getString("account_source"),
                            rs.getBoolean("hotlisted")
                    )).optional().orElse(null);
        } catch (IllegalArgumentException e) {
            log.error("DB GET account by id failed", e);
            throw e;
        } catch (DataAccessException e) {
            log.error("DB GET account by id failed", e);
            throw e;
        }
    }

    public String create(Map<String, Object> values) {
        try {
            log.info("DB INSERT sa_accounts");
            UUID id = UUID.randomUUID();
            values.put("id", id);
            values.put("hotlisted", false);
            values.put("created_at", OffsetDateTime.now());
            values.put("updated_at", OffsetDateTime.now());
            jdbc.sql("""
                    insert into public.sa_accounts
                    (id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street,
                     mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted, created_at, updated_at)
                    values (:id, :salutation, :first_name, :last_name, :person_email, :person_birthdate, :phone, :mobile_phone,
                            :mailing_street, :mailing_postal_code, :mailing_city, :mailing_country, :account_status, :account_source,
                            :hotlisted, :created_at, :updated_at)
                    """).params(values).update();
            log.info("DB INSERT sa_accounts outcome rows=1");
            return id.toString();
        } catch (DataAccessException e) {
            log.error("DB INSERT sa_accounts failed", e);
            throw e;
        }
    }

    public String update(String id, Map<String, Object> values) {
        try {
            log.info("DB UPDATE sa_accounts");
            List<String> sets = new ArrayList<>();
            Map<String, Object> params = new HashMap<>(values);
            params.put("id", UUID.fromString(id));
            params.put("updated_at", OffsetDateTime.now());
            if (values.containsKey("salutation")) sets.add("salutation = :salutation");
            if (values.containsKey("first_name")) sets.add("first_name = :first_name");
            if (values.containsKey("last_name")) sets.add("last_name = :last_name");
            if (values.containsKey("person_email")) sets.add("person_email = :person_email");
            if (values.containsKey("person_birthdate")) sets.add("person_birthdate = :person_birthdate");
            if (values.containsKey("phone")) sets.add("phone = :phone");
            if (values.containsKey("mobile_phone")) sets.add("mobile_phone = :mobile_phone");
            if (values.containsKey("mailing_street")) sets.add("mailing_street = :mailing_street");
            if (values.containsKey("mailing_postal_code")) sets.add("mailing_postal_code = :mailing_postal_code");
            if (values.containsKey("mailing_city")) sets.add("mailing_city = :mailing_city");
            if (values.containsKey("mailing_country")) sets.add("mailing_country = :mailing_country");
            if (values.containsKey("account_status")) sets.add("account_status = :account_status");
            if (values.containsKey("account_source")) sets.add("account_source = :account_source");
            sets.add("updated_at = :updated_at");
            int updated = jdbc.sql("update public.sa_accounts set " + String.join(", ", sets) + " where id = :id")
                    .params(params)
                    .update();
            log.info("DB UPDATE sa_accounts outcome rows={}", updated);
            return id;
        } catch (DataAccessException e) {
            log.error("DB UPDATE sa_accounts failed", e);
            throw e;
        }
    }
}
