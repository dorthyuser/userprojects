package com.ai2dev.bustravelaccounts.repository;

import com.ai2dev.bustravelaccounts.dto.request.AccountRequest;
import com.ai2dev.bustravelaccounts.dto.response.AccountResponse;
import java.util.List;
import java.util.Optional;
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
        log.info("DB GET /accounts");
        return jdbc.sql("select id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted from public.sa_accounts where lower(person_email) = lower(:email) order by id")
                .param("email", email)
                .query((rs, rowNum) -> new AccountResponse(UUID.fromString(rs.getString("id")), rs.getString("salutation"), rs.getString("first_name"), rs.getString("last_name"), rs.getString("person_email"), rs.getObject("person_birthdate", java.time.LocalDate.class), rs.getString("phone"), rs.getString("mobile_phone"), rs.getString("mailing_street"), rs.getString("mailing_postal_code"), rs.getString("mailing_city"), rs.getString("mailing_country"), rs.getString("account_status"), rs.getString("account_source"), rs.getObject("hotlisted", Boolean.class)))
                .list();
    }

    public Optional<AccountResponse> findById(String id) {
        log.info("DB GET /accounts/{}", id);
        return jdbc.sql("select id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted from public.sa_accounts where id = :id")
                .param("id", UUID.fromString(id))
                .query((rs, rowNum) -> new AccountResponse(UUID.fromString(rs.getString("id")), rs.getString("salutation"), rs.getString("first_name"), rs.getString("last_name"), rs.getString("person_email"), rs.getObject("person_birthdate", java.time.LocalDate.class), rs.getString("phone"), rs.getString("mobile_phone"), rs.getString("mailing_street"), rs.getString("mailing_postal_code"), rs.getString("mailing_city"), rs.getString("mailing_country"), rs.getString("account_status"), rs.getString("account_source"), rs.getObject("hotlisted", Boolean.class)))
                .optional();
    }

    public UUID insert(AccountRequest request) {
        log.info("DB POST /accounts");
        UUID id = UUID.randomUUID();
        try {
            jdbc.sql("insert into public.sa_accounts (id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source) values (:id, :salutation, :first_name, :last_name, :person_email, :person_birthdate, :phone, :mobile_phone, :mailing_street, :mailing_postal_code, :mailing_city, :mailing_country, :account_status, :account_source)")
                    .param("id", id)
                    .param("salutation", request.salutation())
                    .param("first_name", request.firstName())
                    .param("last_name", request.lastName())
                    .param("person_email", request.personEmail())
                    .param("person_birthdate", request.personBirthdate())
                    .param("phone", request.phone())
                    .param("mobile_phone", request.mobilePhone())
                    .param("mailing_street", request.mailingStreet())
                    .param("mailing_postal_code", request.mailingPostalCode())
                    .param("mailing_city", request.mailingCity())
                    .param("mailing_country", request.mailingCountry())
                    .param("account_status", request.accountStatus())
                    .param("account_source", request.accountSource())
                    .update();
            return id;
        } catch (DataAccessException ex) {
            log.error("DB insert failed", ex);
            throw ex;
        }
    }

    public UUID update(String id, AccountRequest request) {
        log.info("DB PUT /accounts/{}", id);
        try {
            StringBuilder sql = new StringBuilder("update public.sa_accounts set ");
            sql.append("salutation = :salutation, first_name = :first_name, last_name = :last_name, person_email = :person_email, mailing_street = :mailing_street, mailing_postal_code = :mailing_postal_code, mailing_city = :mailing_city, mailing_country = :mailing_country, account_status = :account_status, account_source = :account_source, updated_at = now()");
            if (request.personBirthdate() != null) sql.append(", person_birthdate = :person_birthdate");
            if (request.phone() != null) sql.append(", phone = :phone");
            if (request.mobilePhone() != null) sql.append(", mobile_phone = :mobile_phone");
            sql.append(" where id = :id");
            jdbc.sql(sql.toString())
                    .param("id", UUID.fromString(id))
                    .param("salutation", request.salutation())
                    .param("first_name", request.firstName())
                    .param("last_name", request.lastName())
                    .param("person_email", request.personEmail())
                    .param("mailing_street", request.mailingStreet())
                    .param("mailing_postal_code", request.mailingPostalCode())
                    .param("mailing_city", request.mailingCity())
                    .param("mailing_country", request.mailingCountry())
                    .param("account_status", request.accountStatus())
                    .param("account_source", request.accountSource())
                    .param("person_birthdate", request.personBirthdate())
                    .param("phone", request.phone())
                    .param("mobile_phone", request.mobilePhone())
                    .update();
            return UUID.fromString(id);
        } catch (DataAccessException ex) {
            log.error("DB update failed", ex);
            throw ex;
        }
    }
}
