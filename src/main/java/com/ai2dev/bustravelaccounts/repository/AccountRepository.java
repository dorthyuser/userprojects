package com.ai2dev.bustravelaccounts.repository;

import com.ai2dev.bustravelaccounts.dto.request.AccountRequest;
import com.ai2dev.bustravelaccounts.dto.response.AccountResponse;
import com.ai2dev.bustravelaccounts.exception.BadRequestException;
import com.ai2dev.bustravelaccounts.exception.NotFoundException;
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
import org.springframework.jdbc.core.simple.JdbcClient;
import org.springframework.stereotype.Repository;

@Repository
public class AccountRepository {
    private static final Logger log = LoggerFactory.getLogger(AccountRepository.class);
    private final JdbcClient jdbc;

    public AccountRepository(@Lazy DataSource dataSource) {
        this.jdbc = JdbcClient.create(dataSource);
    }

    public List<AccountResponse> findByEmail(String email) {
        log.info("DB GET accounts by email");
        try {
            return jdbc.sql("select id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted from public.sa_accounts where lower(person_email) = lower(:email) order by id")
                    .param("email", email)
                    .query((rs, rowNum) -> map(rs))
                    .list();
        } catch (DataAccessException ex) {
            log.error("DB GET accounts by email failed", ex);
            throw ex;
        }
    }

    public AccountResponse findById(String id) {
        log.info("DB GET account by id");
        try {
            return jdbc.sql("select id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted from public.sa_accounts where id = :id")
                    .param("id", UUID.fromString(id))
                    .query((rs, rowNum) -> map(rs))
                    .optional()
                    .orElse(null);
        } catch (IllegalArgumentException ex) {
            throw new NotFoundException("Account not found");
        } catch (DataAccessException ex) {
            log.error("DB GET account by id failed", ex);
            throw ex;
        }
    }

    public String insert(AccountRequest request) {
        log.info("DB INSERT account");
        try {
            UUID id = UUID.randomUUID();
            Map<String, Object> params = new HashMap<>();
            params.put("id", id);
            params.put("salutation", request.salutation());
            params.put("first_name", request.firstName());
            params.put("last_name", request.lastName());
            params.put("person_email", request.personEmail());
            params.put("person_birthdate", request.personBirthdate() == null ? null : LocalDate.parse(request.personBirthdate()));
            params.put("phone", request.phone());
            params.put("mobile_phone", request.mobilePhone());
            params.put("mailing_street", request.mailingStreet());
            params.put("mailing_postal_code", request.mailingPostalCode());
            params.put("mailing_city", request.mailingCity());
            params.put("mailing_country", request.mailingCountry());
            params.put("account_status", request.accountStatus());
            params.put("account_source", request.accountSource());
            jdbc.sql("insert into public.sa_accounts (id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source) values (:id, :salutation, :first_name, :last_name, :person_email, :person_birthdate, :phone, :mobile_phone, :mailing_street, :mailing_postal_code, :mailing_city, :mailing_country, :account_status, :account_source)")
                    .params(params)
                    .update();
            return id.toString();
        } catch (DataAccessException ex) {
            log.error("DB INSERT account failed", ex);
            throw ex;
        }
    }

    public String update(String id, AccountRequest request) {
        log.info("DB UPDATE account");
        try {
            List<String> sets = new ArrayList<>();
            Map<String, Object> params = new HashMap<>(Map.of("id", UUID.fromString(id)));
            if (request.salutation() != null) { sets.add("salutation = :salutation"); params.put("salutation", request.salutation()); }
            if (request.firstName() != null) { sets.add("first_name = :first_name"); params.put("first_name", request.firstName()); }
            if (request.lastName() != null) { sets.add("last_name = :last_name"); params.put("last_name", request.lastName()); }
            if (request.personEmail() != null) { sets.add("person_email = :person_email"); params.put("person_email", request.personEmail()); }
            if (request.personBirthdate() != null) { sets.add("person_birthdate = :person_birthdate"); params.put("person_birthdate", LocalDate.parse(request.personBirthdate())); }
            if (request.phone() != null) { sets.add("phone = :phone"); params.put("phone", request.phone()); }
            if (request.mobilePhone() != null) { sets.add("mobile_phone = :mobile_phone"); params.put("mobile_phone", request.mobilePhone()); }
            if (request.mailingStreet() != null) { sets.add("mailing_street = :mailing_street"); params.put("mailing_street", request.mailingStreet()); }
            if (request.mailingPostalCode() != null) { sets.add("mailing_postal_code = :mailing_postal_code"); params.put("mailing_postal_code", request.mailingPostalCode()); }
            if (request.mailingCity() != null) { sets.add("mailing_city = :mailing_city"); params.put("mailing_city", request.mailingCity()); }
            if (request.mailingCountry() != null) { sets.add("mailing_country = :mailing_country"); params.put("mailing_country", request.mailingCountry()); }
            if (request.accountStatus() != null) { sets.add("account_status = :account_status"); params.put("account_status", request.accountStatus()); }
            if (request.accountSource() != null) { sets.add("account_source = :account_source"); params.put("account_source", request.accountSource()); }
            sets.add("updated_at = now()");
            if (sets.size() == 1) {
                throw new BadRequestException("MULE UNKNOWN ERROR");
            }
            int updated = jdbc.sql("update public.sa_accounts set " + String.join(", ", sets) + " where id = :id")
                    .params(params)
                    .update();
            if (updated == 0) {
                throw new NotFoundException("Account not found");
            }
            return id;
        } catch (IllegalArgumentException ex) {
            throw new NotFoundException("Account not found");
        } catch (DataAccessException ex) {
            log.error("DB UPDATE account failed", ex);
            throw ex;
        }
    }

    public boolean existsByEmail(String email) {
        return !findByEmail(email).isEmpty();
    }

    private AccountResponse map(ResultSet rs) throws SQLException {
        return new AccountResponse(
                rs.getString("id"),
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
                rs.getBoolean("hotlisted"));
    }
}
