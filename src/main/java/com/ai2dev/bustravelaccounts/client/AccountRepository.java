package com.ai2dev.bustravelaccounts.client;

import com.ai2dev.bustravelaccounts.model.request.AccountRequest;
import com.ai2dev.bustravelaccounts.model.response.AccountResponse;
import com.ai2dev.bustravelaccounts.model.response.IdResponse;
import java.util.List;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.jdbc.core.simple.JdbcClient;
import org.springframework.stereotype.Component;

@Component
public class AccountRepository {
    private static final Logger log = LoggerFactory.getLogger(AccountRepository.class);
    private final JdbcClient jdbcClient;

    public AccountRepository(JdbcClient jdbcClient) {
        this.jdbcClient = jdbcClient;
    }

    public List<AccountResponse> findByEmail(String email) {
        log.info("DB SELECT public.sa_accounts by email");
        List<AccountResponse> rows = jdbcClient.sql("""
                select id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone,
                       mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted
                from public.sa_accounts
                where lower(person_email) = lower(:email)
                """)
                .param("email", email)
                .query((rs, rowNum) -> new AccountResponse(
                        rs.getObject("id", java.util.UUID.class).toString(),
                        rs.getString("salutation"),
                        rs.getString("first_name"),
                        rs.getString("last_name"),
                        rs.getObject("person_birthdate", java.time.LocalDate.class),
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
        log.info("DB SELECT public.sa_accounts by email rows={}", rows.size());
        return rows;
    }

    public AccountResponse findById(String id) {
        log.info("DB SELECT public.sa_accounts by id");
        List<AccountResponse> rows = jdbcClient.sql("""
                select id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone,
                       mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted
                from public.sa_accounts
                where id = :id
                """)
                .param("id", java.util.UUID.fromString(id))
                .query((rs, rowNum) -> new AccountResponse(
                        rs.getObject("id", java.util.UUID.class).toString(),
                        rs.getString("salutation"),
                        rs.getString("first_name"),
                        rs.getString("last_name"),
                        rs.getObject("person_birthdate", java.time.LocalDate.class),
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
        log.info("DB SELECT public.sa_accounts by id rows={}", rows.size());
        return rows.isEmpty() ? null : rows.get(0);
    }

    public IdResponse create(AccountRequest request) {
        log.info("DB INSERT public.sa_accounts");
        java.util.UUID id = java.util.UUID.randomUUID();
        int updated = jdbcClient.sql("""
                insert into public.sa_accounts (
                    id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone,
                    mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source
                ) values (
                    :id, :salutation, :firstName, :lastName, :personEmail, :personBirthdate, :phone, :mobilePhone,
                    :mailingStreet, :mailingPostalCode, :mailingCity, :mailingCountry, :accountStatus, :accountSource
                )
                """)
                .param("id", id)
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
        log.info("DB INSERT public.sa_accounts rows={}", updated);
        return new IdResponse(id.toString());
    }

    public IdResponse update(String id, AccountRequest request) {
        log.info("DB UPDATE public.sa_accounts");
        java.util.List<String> sets = new java.util.ArrayList<>();
        java.util.Map<String, Object> params = new java.util.HashMap<>();
        params.put("id", java.util.UUID.fromString(id));
        if (request.salutation() != null) { sets.add("salutation = :salutation"); params.put("salutation", request.salutation()); }
        if (request.firstName() != null) { sets.add("first_name = :firstName"); params.put("firstName", request.firstName()); }
        if (request.lastName() != null) { sets.add("last_name = :lastName"); params.put("lastName", request.lastName()); }
        if (request.personEmail() != null) { sets.add("person_email = :personEmail"); params.put("personEmail", request.personEmail()); }
        if (request.personBirthdate() != null) { sets.add("person_birthdate = :personBirthdate"); params.put("personBirthdate", request.personBirthdate()); }
        if (request.phone() != null) { sets.add("phone = :phone"); params.put("phone", request.phone()); }
        if (request.mobilePhone() != null) { sets.add("mobile_phone = :mobilePhone"); params.put("mobilePhone", request.mobilePhone()); }
        if (request.mailingStreet() != null) { sets.add("mailing_street = :mailingStreet"); params.put("mailingStreet", request.mailingStreet()); }
        if (request.mailingPostalCode() != null) { sets.add("mailing_postal_code = :mailingPostalCode"); params.put("mailingPostalCode", request.mailingPostalCode()); }
        if (request.mailingCity() != null) { sets.add("mailing_city = :mailingCity"); params.put("mailingCity", request.mailingCity()); }
        if (request.mailingCountry() != null) { sets.add("mailing_country = :mailingCountry"); params.put("mailingCountry", request.mailingCountry()); }
        if (request.accountStatus() != null) { sets.add("account_status = :accountStatus"); params.put("accountStatus", request.accountStatus()); }
        if (request.accountSource() != null) { sets.add("account_source = :accountSource"); params.put("accountSource", request.accountSource()); }
        sets.add("updated_at = now()");
        String sql = "update public.sa_accounts set " + String.join(", ", sets) + " where id = :id";
        int updated = jdbcClient.sql(sql).params(params).update();
        log.info("DB UPDATE public.sa_accounts rows={}", updated);
        return new IdResponse(id);
    }
}
